import { FileChange } from "./FileChange";
import { NodeOf } from "./NodeOf";
import { PullRequestState } from "./PullRequestState";

export type Pull = {
  readonly body: string;
  readonly checksUrl: string;
  readonly changedFiles: number;
  readonly state: PullRequestState;
  readonly files: {
    readonly edges: NodeOf<FileChange>[];
  };
};
