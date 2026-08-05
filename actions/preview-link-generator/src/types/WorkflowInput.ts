import { getInput } from "@actions/core";

export class WorkflowInput {
    get collapsibleAfter(): number {
        const val = parseInt(
            getInput("collapsible_after", { required: false }) || "10"
        );
        return val || 10;
    }

    get repoToken(): string {
        const val = getInput("repo_token", { required: true });
        return val;
    }

    get maxRowCount(): number {
        const val = getInput("max_row_count");
        return parseInt(val || "30");
    }

    get maxWaitTimeMinutes(): number {
        const val = parseInt(getInput("max_wait_time_minutes") || "20");
        return val > 0 ? val : 20;
    }

    constructor() {}
}

export const workflowInput: WorkflowInput = new WorkflowInput();
