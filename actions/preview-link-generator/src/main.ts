import { setFailed } from "@actions/core";
import { tryUpdatePullRequestBody } from "./pull-updater";
import { workflowInput } from "./types/WorkflowInput";

async function run(): Promise<void> {
    try {
        const token: string = workflowInput.repoToken;
        await tryUpdatePullRequestBody(token);
    } catch (error: unknown) {
        const e = error as Error;
        setFailed(e.message);
    }
}

run();
