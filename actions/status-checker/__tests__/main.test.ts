import { wait } from "../src/wait";
import { env, execPath } from "process";
import { execFile, ExecFileOptions } from "child_process";
import { join } from "path";

describe("status-checker", () => {
  it("throws invalid number", async () => {
    const input = parseInt("foo", 10);
    await expect(wait(input)).rejects.toThrow("milliseconds not a number");
  });
  
  it("waits 500 ms", async () => {
    const start = new Date();
    await wait(500);
    const end = new Date();
    var delta = Math.abs(end.getTime() - start.getTime());
    expect(delta).toBeGreaterThan(450);
  });
  
  // Shows how the runner will run a javascript action with env / stdout protocol
  it("runs test", async () => {
    env["INPUT_MILLISECONDS"] = "500";
    const np = execPath;
    const ip = join(__dirname, "..", "lib", "main.js");
    const options: ExecFileOptions = {
      env: env,
    };
    console.log(await execFile(np, [ip], options).toString());
  });
});
