import { AuthenticationSession, authentication } from "vscode";
import { ConfigReader } from "../configuration/config-reader";

export async function getGitHubSessionToken(): Promise<string | undefined> {
    let session: AuthenticationSession | undefined;

    const config = ConfigReader.readConfig();
    
    if (config.allowGitHubSession) {
        const providerId = "github";
        const accounts = await authentication.getAccounts(providerId);
        if (accounts && accounts.length > 0) {
            session = await authentication.getSession(
                providerId,
                ["repo"], {
                account: accounts[0],
                createIfNone: true
            });
        }
    }

    return session?.accessToken ?? undefined;
}