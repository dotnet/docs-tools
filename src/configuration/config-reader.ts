import { workspace, WorkspaceConfiguration } from "vscode";
import { AppConfig } from "./types/AppConfig";
import { toolName } from "../consts";

/**
 * Reads the configuration settings for the extension.
 */
export class ConfigReader {
    private static appConfig?: AppConfig = undefined;
    
    /**
     * Reads the configuration settings for the extension.
     * @returns The configuration settings for the extension.
     */
    public static readConfig = (): AppConfig => {
        if (ConfigReader.appConfig) {
            return ConfigReader.appConfig;
        }
        
        const config: WorkspaceConfiguration = workspace.getConfiguration(toolName);
        
        ConfigReader.appConfig = new AppConfig(config);

        return ConfigReader.appConfig;
    };
}