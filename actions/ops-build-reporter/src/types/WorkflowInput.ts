import { getInput } from "@actions/core";

export class WorkflowInput {
    get collapsibleAfter(): number {
        const val = getInput("collapsible_after", { required: false });
        return parseInt(val);
    }

    get repoToken(): string {
        const val = getInput("repo_token", { required: true });
        return val;
    }

    get maxRowCount(): number {
        const val = getInput("max_row_count");
        return parseInt(val);
    }

    get maxWaitTimeMinutes(): number {
        const val = parseInt(getInput("max_wait_time_minutes"));
        return val > 0 ? val : 20;
    }

    get annotateFiles(): boolean {
        return getInput("annotate_file_warnings").toLowerCase() === "true";
    }

    constructor() {}
}

export const workflowInput: WorkflowInput = new WorkflowInput();
