import { wait } from "./wait"
import { checkStatus } from "./status-checker"
import { getInput, setFailed } from "@actions/core";

async function run(): Promise<void> {
  try {
    const token: string = getInput("repo-token");

    // Wait 60 seconds before checking status check result.
    await wait(60000);
    console.log("Waited 60 seconds.");

    await checkStatus(token);
  } catch (error: unknown) {
    const e = error as Error;
    setFailed(e.message);
  }
};

run();
