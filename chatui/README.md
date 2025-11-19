# Chat UI - AI Assistant for Expense Management

This folder contains the AI-powered chat interface for the Expense Management System.

## Features

- Natural language interaction with the expense system
- Create expenses through conversation
- Query and search expenses
- Get insights and summaries
- Retrieval-Augmented Generation (RAG) pattern for contextual understanding

## Implementation

The chat UI is integrated into the main ASP.NET application with the following components:

1. **ChatService.cs**: Handles Azure OpenAI integration and function calling
2. **Chat.cshtml**: User interface for the chat experience
3. **API Integration**: Seamlessly calls expense management APIs based on user requests

## RAG Context

The `/chatui/RAG` folder contains contextual information that enhances the AI's understanding:
- Expense system policies
- Category guidelines
- Approval workflows

## Configuration

Chat features require Azure OpenAI to be deployed. Set `DEPLOY_GENAI=true` in `deploy.sh` to enable.

Configuration is stored in:
- `GenAISettings.json` (created during deployment)
- Environment variables in App Service

## Usage

Users can access the chat interface by:
1. Clicking the chat button (💬) in the bottom-right corner of the main dashboard
2. Navigating directly to `/Chat`

The chat assistant can:
- "Show me my pending expenses"
- "Create a new travel expense for £50"
- "What's my total spending this month?"
- "Help me understand the approval process"
