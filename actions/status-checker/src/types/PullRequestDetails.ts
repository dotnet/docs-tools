import { Pull } from "./Pull";

export type PullRequestDetails = {
  readonly data: {
    readonly repository: {
      readonly pullRequest: Pull
    };
  }
}
