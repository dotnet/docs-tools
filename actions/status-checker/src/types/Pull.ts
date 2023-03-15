import { FileChange } from "./FileChange";
import { NodeOf } from "./NodeOf";

export type Pull = {
  readonly body: string;
  readonly checksUrl: string;
  readonly changedFiles: number;
  readonly files: {
    readonly edges: NodeOf<FileChange>[];
  };
};
