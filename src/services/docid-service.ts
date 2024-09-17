import fetch from "node-fetch";
import { parseStringPromise } from "xml2js";
import { ItemType } from "../commands/types/ItemType";

export class DocIdService {
    public static async getDocId(displayName: string, apiType: ItemType, gitUrl: string): Promise<string | null> {

        if (apiType === ItemType.namespace) {
            return displayName;
        }

        const response = await fetch(gitUrl, {
            headers: {
                // eslint-disable-next-line @typescript-eslint/naming-convention
                "Content-Type": "application/xml",
            }
        });
        if (!response.ok) {
            return null;
        }

        const text = await response.text();
        const xml = await parseStringPromise(text);

        if ([ItemType.class, ItemType.struct, ItemType.interface, ItemType.enum].includes(apiType)) {
            const typeSignature = xml.Type.TypeSignature?.find((x: any) => x.$.Language === 'DocId');
            return typeSignature ? typeSignature.$.Value.substring(2) : null;
        }

        const memberType = apiType;
        const typeName = xml.Type.$.FullName;
        let memberName = displayName.substring(typeName.length + 1).split('(')[0];

        if (apiType === ItemType.constructor || apiType === ItemType.method) {
            if (apiType === ItemType.constructor) {
                memberName = ".ctor";
            }

            const paramList = displayName.split('(')[1].slice(0, -1);
            const paramTypes = paramList.length > 0 ? paramList.split(',').map(x => x.trim().split(' ')[0]) : [];
            const numParams = paramTypes.length;

            // No parameters.
            if (numParams === 0) {
                const methodOrCtor = xml.Type.Members[0].Member?.find((x: any) =>
                    x.$.MemberName === memberName &&
                    x.MemberType[0] === memberType &&
                    !x.Parameters.Parameter
                );

                return methodOrCtor ? methodOrCtor.MemberSignature.find((x: any) => x.$.Language === 'DocId').$.Value.substring(2) : null;
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
                    if (paramTypes[paramIndex] !== parameter.$.Type.split('.').pop()) {
                        break;
                    }
                    paramIndex++;
                }

                if (paramIndex === numParams) {
                    // We found a match.
                    return candidate.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2);
                }
            }

            // We didn't find a matching method/constructor.
            return null;
        }

        // Property, Event, Field
        const member = xml.Type.Members[0].Member?.find((x: any) =>
            x.$.MemberName === memberName &&
            x.MemberType[0] === memberType
        );

        return member ? member.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) : null;
    }
}
