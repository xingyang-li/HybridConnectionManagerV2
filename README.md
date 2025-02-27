# Hybrid Connection Manager V2

<!-- GETTING STARTED -->
## Getting Started

This project manages Hybrid Connections for Azure Relay. It is a complete rewrite of the original Hybrid Connection Manager project. The new version is built using .NET 8 and gRPC, and it has a new GUI and CLI.

### Prerequisites

 Clone the repo
   ```sh
   git clone https://github.com/xingyang-li/HybridConnectionManagerV2.git
   ```
 Install ElectronNET.CLI package
   ```sh
   dotnet tool install ElectronNET.CLI -g
   ```

### Build the Solution

Build the solution at the root of the repo using Visual Studio or `dotnet build` command. This will build the GUI, CLI, and gRPC server projects.

### Run the Hybrid Connection Manager Service

The Hybrid Connection Manager Service is a gRPC server that runs on the local machine and manages Hybrid Connection Listeners and proxies TCP requests to specified local endpoints. The service handles gRPC API requests initiated from the GUI or CLI to modify the Hybrid Connection. To run the service, open a console window and navigate to the build output directory for the HybridConnectionManagerService project:
```sh
cd HybridConnectionManagerService/bin/Debug/netx.0
```

Then run the following command:
```sh
HybridConnectionManagerService.exe
```

### Hybrid Connection Manager CLI

The Hybrid Connection Manager CLI is a command line interface that allows you to interact with the Hybrid Connection Manager Service by sending it gRPC API requests. To run the CLI, open a console window and navigate to the build output directory for the HybridConnectionManagerCLI project:
```sh
cd HybridConnectionManagerCLI/bin/Debug/netx.0
```

Then you can run the `hcm` executable to interact with the backend service. For example, to list the Hybrid Connections that exist on the local machine, run:
```sh
hcm list
```

### Hybrid Connection Manager GUI

The Hybrid Connection Manager GUI is a UI that allows you to interact with the Hybrid Connection Manager Service using gRPC API requests. It is an ASP.NET MVC web application that has a dotnet Electron app wrapper. There are two easy ways to run the UI: 

#### 1. Start development server with Visual Studio

Open the solution file in Visual Studio and run the `HybridConnectionManagerGUI` startup item. This will start a development server that serves the GUI and the web app content will be served in your default browser.

#### 2. Build and run the Electron app

Navigate to the root directory for the HybridConnectionManagerGUI project:
```sh
cd HybridConnectionManagerGUI
```

Build and start the Electron app with this command:
```sh
electronize start
```

The Electron app will be launched and the web app content will be served in the Electron app.