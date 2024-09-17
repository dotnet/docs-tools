/**
 * The `nameof` operator returns the name of a variable, type, or member as a `string`.
 * @param name The name of the variable, type, or member.
 * @returns A `string` that represents the name of the variable, type, or member.
 */
export const nameof = <T>(name: keyof T): string => name as string;