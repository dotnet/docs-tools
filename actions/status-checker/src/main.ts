import { wait } from "./wait";
import { isSuccessStatus } from "./status-checker";
import { setFailed } from "@actions/core";
import { workflowInput } from "./types/WorkflowInput";

async function run(): Promise<void> {
    try {
        const token: string = workflowInput.repoToken;

        // Wait 60 seconds before checking status check result.
        await wait(60000);
        console.log("Waited 60 seconds.");

        // Wait for success/fail status of the build.
        const isSuccess = await isSuccessStatus(token);
        if (isSuccess) {
            console.log("✅ Build status is good...");
        } else {
            console.log("❌ Build status has warnings or errors!");
        }
    } catch (error: unknown) {
        const e = error as Error;
        setFailed(e.message);
    }
}

run();
