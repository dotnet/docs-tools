import fetch from "node-fetch";
import { parseStringPromise } from "xml2js";
import { ItemType } from "../commands/types/ItemType";
import { HeadersInit } from "node-fetch";
import { parse } from "yaml";
import { getGitHubSessionToken } from "./github-auth-service";

export interface DocIdResult {
    docId?: string;
    severity?: "error" | "warning";
    message?: string;
}

type ParserFactory = (text: string, displayName: string, apiType: ItemType, gitUrl: string) => Promise<DocIdResult>;

export class DocIdService {
    public static async getDocId(displayName: string, apiType: ItemType, gitUrl: string): Promise<DocIdResult> {

        // For namespaces, we don't even look them up.
        if (apiType === ItemType.namespace) {
            return { docId: displayName };
        }

        const response = await getResponse(gitUrl);
        if (!response.ok) {

            if (response.status === 404) {
                return {
                    severity: "error",
                    message: `HTTP 404: Unable to retrieve xref metadata for "${displayName}". When this happens, it's usually because the GitHub repo for the API docs is private.`
                };
            }

            return {
                severity: "warning",
                message: `HTTP ${response.status}: Failed to get the DocId for "${displayName}". Attempted fetching [this Raw GitHub URL](${gitUrl}).`
            };
        }

        const text = await response.text();
        const textType = gitUrl.split('.').pop();

        let parserFactory: ParserFactory | undefined;
        switch (textType) {
            case "xml":
                parserFactory = parseXml;
                break;
            case "yml":
                parserFactory = parseYaml;
                break;
        }

        if (!parserFactory) {
            return {
                severity: "error",
                message: `Failed to get the DocId for "${displayName}". Unsupported file type: ${textType}.`
            };
        }

        return parserFactory(text, displayName, apiType, gitUrl);
    }
}

async function getResponse(gitUrl: string) {
    const accessToken = await getGitHubSessionToken();

    const headers: HeadersInit = {
        // eslint-disable-next-line @typescript-eslint/naming-convention
        "Content-Type": "application/xml",
    };

    if (accessToken) {
        headers["Authorization"] = `Bearer ${accessToken}`;
    }

    const response = await fetch(gitUrl, {
        headers
    });
    return response;
}

async function parseXml(text: string, displayName: string, apiType: ItemType, gitUrl: string): Promise<DocIdResult> {
    const xml = await parseStringPromise(text);

    // Class, struct, interface, or enum.
    if ([ItemType.class, ItemType.struct, ItemType.interface, ItemType.enum].includes(apiType)) {
        const typeSignature = xml.Type.TypeSignature?.find((x: any) => x.$.Language === 'DocId');
        return { docId: typeSignature ? typeSignature.$.Value.substring(2) : null };
    }

    const memberType = apiType;
    const typeName = xml.Type.$.FullName;
    let memberName = displayName.substring(typeName.length + 1).split('(')[0];

    // Constructor or method.
    if (apiType === ItemType.constructor || apiType === ItemType.method) {
        if (apiType === ItemType.constructor) {
            memberName = ".ctor";
        }

        // All overloads.
        if (displayName.endsWith('*')) {
            if (apiType === ItemType.method) {
                memberName = memberName.substring(0, memberName.length - 1);
            }

            // Match any overload and then modify the DocId.
            const methodOrCtor = xml.Type.Members[0].Member?.find((x: any) =>
                x.$.MemberName === memberName &&
                x.MemberType[0] === memberType
            );

            let docId = methodOrCtor.MemberSignature.find((x: any) => x.$.Language === 'DocId').$.Value.substring(2);
            // Add * (and remove parens and double backticks, if present).
            docId = docId.split('(')[0].split("``")[0];
            return { docId: docId.concat('*') };
        }

        const paramList = displayName.split('(')[1].slice(0, -1);
        const paramTypes = splitParamList(paramList);
        const numParams = paramTypes.length;

        // No parameters.
        if (numParams === 0) {
            const methodOrCtor = xml.Type.Members[0].Member?.find((x: any) =>
                x.$.MemberName === memberName &&
                x.MemberType[0] === memberType &&
                !x.Parameters.Parameter
            );

            return { docId: methodOrCtor ? methodOrCtor.MemberSignature.find((x: any) => x.$.Language === 'DocId').$.Value.substring(2) : null };
        }

        // With parameters.
        const candidates = xml.Type.Members[0].Member?.filter((x: any) =>
            x.$.MemberName === memberName &&
            x.MemberType[0] === memberType &&
            x.Parameters[0].Parameter?.length === numParams
        );

        for (const candidate of candidates) {
            let paramIndex = 0;
            for (const parameter of candidate.Parameters[0].Parameter) {
                let xmlType = parameter.$.Type;

                // Unqualify all type names within the parameter.
                xmlType = unqualifyType(xmlType);

                if (xmlType !== paramTypes[paramIndex]) {
                    // Doesn't match the expected parameter type at this position.
                    break;
                }

                paramIndex++;
            }

            if (paramIndex === numParams) {
                // We found a match.
                return { docId: candidate.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) };
            }
        }

        // We didn't find a matching method/constructor.
        return {
            severity: "warning",
            message: `Failed to get the DocId for "${displayName}". Didn't find a matching method/constructor.`
        };
    }

    // Property, Event, or Field.
    if (apiType === ItemType.property || apiType === ItemType.attachedProperty ||
        apiType === ItemType.event || apiType === ItemType.attachedEvent ||
        apiType === ItemType.field) {

        // Special case for "Item" property (which is actually
        // an indexer, not a property. Go figure.)
        // The displayName is "Item[String]" or "Item[Int32]" etc.,
        // but in the ECMAXML, the MemberName is just "Item".
        // And, this "property" also has parameter(s)!
        if (memberName.split('[')[0] === "Item") {
            const paramList = memberName.split('[')[1].slice(0, -1);
            const paramTypes = paramList.length > 0 ? paramList.split(',').map(x => x.trim().split(' ')[0]) : [];

            const candidates = xml.Type.Members[0].Member?.filter((x: any) =>
                x.$.MemberName === "Item" &&
                x.MemberType[0] === memberType &&
                x.Parameters[0].Parameter?.length === paramTypes.length
            );

            for (const candidate of candidates) {
                let paramIndex = 0;
                for (const parameter of candidate.Parameters[0].Parameter) {
                    let xmlType = parameter.$.Type;

                    if (paramTypes[paramIndex] !== xmlType.split('.').pop()) {
                        break;
                    }

                    paramIndex++;
                }

                if (paramIndex === paramTypes.length) {
                    // We found a match.
                    return { docId: candidate.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) };
                }
            }
        } else {
            const member = xml.Type.Members[0].Member?.find((x: any) =>
                x.$.MemberName === memberName &&
                x.MemberType[0] === memberType
            );

            return { docId: member ? member.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) : null };
        }
    }

    // Operator.
    if (apiType === ItemType.operator) {
        const member = xml.Type.Members[0].Member?.find((x: any) =>
            x.$.MemberName === "op_".concat(memberName) &&
            x.MemberType[0] === "Method" // Operators are "methods" in the ECMAXML.
        );

        return { docId: member ? member.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) : null };
    }

    // We didn't find a matching API.
    return {
        severity: "warning",
        message: `Failed to get the DocId for "${displayName}". Attempted parsing [this Raw GitHub URL](${gitUrl}).`
    };
}

// Splits the param list into individual parameters.
function splitParamList(input: string): string[] {
    const result: string[] = [];
    let current = '';
    let depth = 0;

    for (let i = 0; i < input.length; i++) {
        const char = input[i];

        if (char === '<') {
            depth++;
        } else if (char === '>') {
            depth--;
        } else if (char === ',' && depth === 0) {
            result.push(current.trim());
            current = '';
            continue;
        }

        current += char;
    }

    if (current) {
        result.push(current.trim());
    }

    return result;
}

// Removes fully qualified part of each type name.
function unqualifyType(input: string): string {
    let unqualifiedType = input;

    const regex = /(\w|\.)*/g;

    const matches = input.matchAll(regex);
    for (const match of matches) {
        unqualifiedType = unqualifiedType.replace(match[0], match[0].substring(match[0].lastIndexOf(".") + 1));
    }

    return unqualifiedType;
}

async function parseYaml(text: string, displayName: string, apiType: ItemType, gitUrl: string): Promise<DocIdResult> {
    if (apiType === ItemType.package || apiType === ItemType.module || apiType === ItemType.typeAlias) {
        return { docId: displayName };
    }

    const yml = parse(text);
    if (!yml) {
        return {
            severity: "error",
            message: `Failed to get the DocId for "${displayName}". The YAML file is empty.`
        };
    }

    let uid: string | undefined;

    if (apiType === ItemType.property || apiType === ItemType.attachedProperty) {
        if (yml.properties) {
            let memberName = displayName.split('.').pop();
            uid = yml.properties.find((p: any) => p.name === memberName)?.uid;
        }
    }

    if (apiType === ItemType.method || apiType === ItemType.constructor) {
        if (yml.methods) {
            let memberName = displayName.split('.').pop();
            if (memberName) {
                let methodName = memberName.split('(')[0];
                uid = yml.methods.find((m: any) => {
                        return m.uid === displayName || m.name.startsWith(methodName);
                    })?.uid;
            }
        }
    }

    if (apiType === ItemType.function) {
        if (yml.functions) {
            let memberName = displayName.split('.').pop();
            if (memberName) {
                let functionName = memberName.split('(')[0];
                uid = yml.functions.find((m: any) => {
                        return m.uid === displayName || m.name.startsWith(functionName);
                    })?.uid;
            }
        }
    }

    if (apiType === ItemType.member) {
        if (yml.attributes) {
            let memberName = displayName.split('.').pop();
            uid = yml.attributes.find((m: any) => m.uid === displayName || m.name === memberName)?.uid;
        } else if (yml.variables) {
            let memberName = displayName.split('.').pop();
            uid = yml.variables.find((m: any) => m.uid === displayName || m.name === memberName)?.uid;
        }
    }

    if (apiType === ItemType.class ||
        apiType === ItemType.struct ||
        apiType === ItemType.interface ||
        apiType === ItemType.enum) {
        uid = yml.uid;
    }

    if (uid) {
        return { docId: uid };
    }

    return {
        severity: "error",
        message: `Failed to get the DocId for "${displayName}". The YAML file is missing the 'uid' field.`
    };
}
