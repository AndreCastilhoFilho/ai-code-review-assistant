# AI Code Review Bot

![License](https://img.shields.io/badge/license-MIT-blue.svg)

## Table of Contents

- [Introduction](#introduction)
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)

## Introduction

**AI Code Review Bot** is an AI-powered tool designed to analyze GitHub pull requests and provide insightful feedback on code quality, best practices, and potential issues. The bot utilizes multiple AI models, including **OpenAI's GPT-4o** and **Hugging Face's Mistral model**, to generate meaningful suggestions that help developers improve their code efficiently.

## Features

- 🚀 **Automated Code Review**: Fetches GitHub PRs and provides AI-driven insights.
- 📖 **Best Practices Analysis**: Highlights readability and maintainability concerns.
- 🔍 **Potential Issue Detection**: Identifies possible code issues before merging.
- 🌐 **Minimal API Integration**: Built with ASP.NET 8 Minimal API for high performance.
- 💻 **Blazor UI**: User-friendly web interface for PR analysis.
- 🤖 **Multiple AI Models**: Supports both OpenAI and Hugging Face’s Mistral model for flexibility and cost efficiency.

## Installation

### Prerequisites

- .NET 8 SDK ([Download Here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- Docker (optional for containerized deployment)
- API Keys (see [Configuration](#configuration))

### Clone the Repository

```bash
git clone https://github.com/AndreCastilhoFilho/clean-architecture-minimal-api.git
cd clean-architecture-minimal-api
```

### Install Dependencies

```bash
dotnet restore
```

## Usage

### Running the API

```bash
dotnet run --project src/AIReview.API
```

Access Swagger UI:
```
https://localhost:5001/swagger
```

### Running the Blazor UI

```bash
dotnet run --project src/AIReview.UI
```

Then open:
```
https://localhost:5002
```

## Configuration

Replace API keys in `appsettings.json` as needed.

```json
{
  "GitHub": {
    "Token": "your-github-token"
  },
  "OpenAI": {
    "ApiKey": "your-api-key"
  },
  "HuggingFaceApi": {
    "ApiKey": "your-api-key"
  }
}
```

## Contributing

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Commit your changes (`git commit -m 'Add new feature'`).
4. Push to your branch (`git push origin feature-branch`).
5. Open a Pull Request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [OpenAI](https://openai.com/) for providing AI capabilities.
- [Hugging Face](https://huggingface.co/) for their free-tier Mistral model.
- [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet) for Minimal API and Blazor UI.

---

