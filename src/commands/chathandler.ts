import {
    CancellationToken,
    ChatContext,
    ChatRequest,
    ChatRequestHandler,
    ChatResponseStream,
    Command,
    LanguageModelChatMessage,
    lm,
} from 'vscode';
import { BASE_PROMPT, copyAIStreamToClipboard, getBreakingChangePrompt, MODEL_SELECTOR } from '../consts';
import { getIssue } from '../services/github-api';

export const chatRequestHandler: ChatRequestHandler = async (
    request: ChatRequest,
    context: ChatContext,
    stream: ChatResponseStream,
    token: CancellationToken) => {

    console.log(`Received chat request: ${request.prompt}`);

    let prompt = '';

    if (request.command === 'breaking-changes') {
        const issueUrl = request.prompt;
        const issue = await getIssue(issueUrl);
        if (issue) {
            prompt = getBreakingChangePrompt(issue, issueUrl);
        }
    }

    if (prompt === '') {
        return;
    }

    const models = await lm.selectChatModels(MODEL_SELECTOR);
    if (models?.length > 0) {
        const model = models[0];

        const messages = [
            LanguageModelChatMessage.Assistant(BASE_PROMPT),
            LanguageModelChatMessage.User(prompt),
        ];

        let markdown = '';

        const chatResponse = await model.sendRequest(messages, {}, token);
        for await (const fragment of chatResponse.text) {
            markdown += fragment;
            stream.markdown(fragment);
        }

        const command: Command = {
            title: 'Copy raw AI response to clipboard?',
            command: copyAIStreamToClipboard,
            arguments: [markdown]
        };

        stream.button(command);
    } else {
        stream.markdown(
            `Sorry, but we're not seeing any AI models available. 😟`
        );
    }
};