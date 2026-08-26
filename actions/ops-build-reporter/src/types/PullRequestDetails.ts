export type PullRequestDetails = {
    readonly repository: {
        readonly pullRequest: {
            readonly body: string;
            readonly checksUrl: string;
            readonly changedFiles: number;
            readonly state: "CLOSED" | "MERGED" | "OPEN";
        };
    };
};
