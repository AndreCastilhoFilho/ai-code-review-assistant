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

**AI Code Review Bot** is an AI-powered tool designed to analyze GitHub pull requests and provide insightful feedback on code quality, best practices, and potential issues. By leveraging OpenAI's GPT-4o model, the bot generates suggestions that help developers improve their code efficiently.

## Features

- 🚀 **Automated Code Review**: Fetches GitHub PRs and provides AI-driven insights.
- 📖 **Best Practices Analysis**: Highlights readability and maintainability concerns.
- 🔍 **Potential Issue Detection**: Identifies possible code issues before merging.
- 🌐 **Minimal API Integration**: Built with ASP.NET 8 Minimal API for high performance.
- 💻 **Blazor UI**: User-friendly web interface for PR analysis.

## Installation

### Prerequisites

- .NET 8 SDK ([Download Here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- Docker (optional for containerized deployment)
- OpenAI API Key (see [Configuration](#configuration))

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

To start the API, run the following command:

```bash
dotnet run --project src/AIReview.API
```

Once running, access Swagger UI to test the API:

```
https://localhost:5001/swagger
```

### Running the Blazor UI

To launch the Blazor frontend, execute:

```bash
dotnet run --project src/AIReview.UI
```

Then open:

```
https://localhost:5002
```

## Configuration

### GitHub Token Setup

Before fetching PR details from GitHub, configure your GitHub API token:

1. **Generate a GitHub Personal Access Token**: [Follow the guide](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token).
2. **Set the GitHub Token**:

   - Using environment variables:
     ```bash
     export GITHUB_TOKEN="your-github-token"
     ```
   - Or, add it to `appsettings.json`:
     ```json
     {
       "GitHub": {
         "Token": "your-github-token"
       }
     }
     ```

### OpenAI API Key Setup

Before using the AI-powered features, configure your OpenAI API key:

1. **Obtain API Key**: [Generate a key here](https://platform.openai.com/account/api-keys).
2. **Set the API Key**:

   - Using environment variables:
     ```bash
     export OPENAI_API_KEY="your-api-key"
     ```
   - Or, add it to `appsettings.json`:
     ```json
     {
       "OpenAI": {
         "ApiKey": "your-api-key"
       }
     }
     ```

## Contributing

We welcome contributions! To contribute:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Commit your changes (`git commit -m 'Add new feature'`).
4. Push to your branch (`git push origin feature-branch`).
5. Open a Pull Request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [OpenAI](https://openai.com/) for providing AI capabilities.
- [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet) for Minimal API and Blazor UI.

---

