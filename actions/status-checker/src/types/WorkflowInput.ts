import { getInput } from "@actions/core";

export class WorkflowInput {

    get repoToken(): string {
        const val = getInput("repo_token", { required: true });
        return val;
    }

    constructor() {}
}

export const workflowInput: WorkflowInput = new WorkflowInput();
