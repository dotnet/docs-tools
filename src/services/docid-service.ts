import fetch from "node-fetch";
import { parseStringPromise } from "xml2js";
import { ItemType } from "../commands/types/ItemType";

export class DocIdService {
    public static async getDocId(displayName: string, apiType: ItemType, gitUrl: string): Promise<string | null> {

        if (apiType === ItemType.namespace) {
            return displayName;
        }

        const response = await fetch(gitUrl);
        if (!response.ok) {
            return null;
        }

        const xml = await parseStringPromise(response.text);

        if ([ItemType.class, ItemType.struct, ItemType.interface, ItemType.enum].includes(apiType)) {
            const typeSignature = xml.TypeSignature?.find((x: any) => x.$.Language === 'DocId');
            return typeSignature ? typeSignature.$.Value.substring(2) : null;
        }

        const memberType = apiType;
        const memberName = displayName.split('(')[0].split('.').pop();

        if (apiType === ItemType.constructor || apiType === ItemType.method) {
            let memberNameToUse = memberName;
            if (apiType === ItemType.constructor) {
                memberNameToUse = ".ctor";
            }

            const paramList = displayName.split('(')[1].slice(0, -1);
            const paramTypes = paramList.length > 0 ? paramList.split(',').map(x => x.trim().split(' ')[0]) : [];
            const numParams = paramTypes.length;

            // No parameters.
            if (numParams === 0) {
                const methodOrCtor = xml.Member?.find((x: any) =>
                    x.$.MemberName === memberNameToUse &&
                    x.MemberType[0] === memberType &&
                    !x.Parameter
                );

                return methodOrCtor ? methodOrCtor.MemberSignature.find((x: any) => x.$.Language === 'DocId').$.Value.substring(2) : null;
            }

            // With parameters.
            const candidates = xml.Member?.filter((x: any) =>
                x.$.MemberName === memberNameToUse &&
                x.MemberType[0] === memberType &&
                x.Parameter?.length === numParams
            );

            for (const candidate of candidates) {
                let paramIndex = 0;
                for (const parameter of candidate.Parameter) {
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
        const member = xml.Member?.find((x: any) =>
            x.$.MemberName === memberName &&
            x.MemberType[0] === memberType
        );

        return member ? member.MemberSignature.find((x: any) => x.$.Language === "DocId").$.Value.substring(2) : null;
    }
}