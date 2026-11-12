# NellieAIChatbot

A customer-support chatbot built with **.NET 8**, **Microsoft Semantic Kernel** and **OpenAI**.
It answers questions about a fictional e-bike retailer, Nelisiwe Bikes, using retrieval-augmented
generation (RAG) over local documents, and includes an evaluation harness for measuring answer quality.

## Features

- **Streaming chat** with multi-turn conversation history
- **RAG**: markdown documents are chunked, embedded with OpenAI embeddings, and searched by cosine similarity
- **Function calling**: the model decides when to call Semantic Kernel plugins ('Knowledge.search', 'DateTime.today', 'DateTime.days_between')
- **Prompt engineering**: the system prompt uses role, grounding rules, output-format constraints and a few-shot example ('Prompts/SystemPrompt.txt') 
- **Filters**: a function-invocation filter logs each tool call so the orchestration is visible
- **Evaluation**: '--eval' runs a question set and scores each answer 1-5 with an LLM judge
