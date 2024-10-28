import {
    CancellationToken,
    ChatContext,
    ChatRequest,
    ChatRequestHandler,
    ChatResponseStream,
    env,
    LanguageModelChatMessage,
    lm,
} from 'vscode';
import { BASE_PROMPT, getBreakingChangePrompt, MODEL_SELECTOR } from '../consts';
import { getIssue } from '../services/github-api';

const textDecoder = new TextDecoder();

export const chatRequestHandler: ChatRequestHandler = async (
    request: ChatRequest,
    context: ChatContext,
    stream: ChatResponseStream,
    token: CancellationToken) => {

    console.log(`Received chat request: ${request.prompt}`);

    const userPrompt = request.prompt;
    let prompt = '';

    if (request.command === 'breaking-changes') {
        const issue = await getIssue(userPrompt);
        if (issue) {
            prompt = getBreakingChangePrompt(issue);
        }
    }

    const [model] = await lm.selectChatModels(MODEL_SELECTOR);
    if (model) {
        const messages = [
            LanguageModelChatMessage.Assistant(BASE_PROMPT),
            LanguageModelChatMessage.User(prompt),
        ];

        const chatResponse = await model.sendRequest(messages, {}, token);
        for await (const fragment of chatResponse.text) {
            stream.markdown(fragment);
        }
    }

    return;
};