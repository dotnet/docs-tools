import { getInput } from "@actions/core";

export class Options {
  get collapsibleAfter(): number {
    const val = parseInt(
      getInput("collapsible_after", { required: false }) || "10"
    );
    return val || 10;
  }

  get docsPath(): string {
    const val = getInput("docs_path", { required: true });
    return val || "docs";
  }

  get urlBasePath(): string {
    const val = getInput("url_base_path", { required: true });
    return val || "dotnet";
  }
  constructor() {}
}

export const options: Options = new Options();
