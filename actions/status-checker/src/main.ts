import { wait } from "./wait";
import { isSuccessStatus } from "./status-checker";
import { getInput, setFailed } from "@actions/core";
import { tryUpdatePullRequestBody } from "./pull-updater";

async function run(): Promise<void> {
  try {
    const token: string = getInput("repo-token");

    // Wait 60 seconds before checking status check result.
    await wait(60000);
    console.log("Waited 60 seconds.");

    // When the status is passed, try to update the PR body.
    const isSuccess = await isSuccessStatus(token);
    if (isSuccess) {
      await tryUpdatePullRequestBody(token);
    } else {
      console.log('Unsuccessful status detected.');
    }
  } catch (error: unknown) {
    const e = error as Error;
    setFailed(e.message);
  }
}

run();
