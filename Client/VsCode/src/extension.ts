"use strict";

import * as vscode from "vscode";
import * as languageClient from "vscode-languageclient";
import * as path from "path";
import * as fs from "fs";

const languageServerPaths = [
	"server/rhetosLanguageServer.dll",
	"../../rhetosLanguageServer/bin/Debug/netcoreapp2.0/rhetosLanguageServer.dll",
]

function activateLanguageServer(context: vscode.ExtensionContext) {
	let serverModule: string = null;
	for (let p of languageServerPaths) {
		p = context.asAbsolutePath(p);

		if (fs.existsSync(p)) {
			serverModule = p;
			break;
		}
	}

	checkRhetosServerSetup(context, ()=>{
		if (!serverModule) throw new URIError("Cannot find the language server module.");
		let workPath = path.dirname(serverModule);

		let serverOptions: languageClient.ServerOptions = {
			run: { command: "dotnet", args: [serverModule], options: { cwd: workPath } },
			debug: { command: "dotnet", args: [serverModule, "--debug"], options: { cwd: workPath } }
		}

		let clientOptions: languageClient.LanguageClientOptions = {
			documentSelector: ["rhetos"],
			synchronize: {
				configurationSection: "rhetosLanguageServer",
				fileEvents: [
					vscode.workspace.createFileSystemWatcher("**/.rhe"),
				]
			},
		}
	
		let client = new languageClient.LanguageClient("rhetosLanguageServer", "Rhetos Language Server", serverOptions, clientOptions);
		let disposable = client.start();
	
		context.subscriptions.push(disposable);
	});
}

export function checkRhetosServerSetup(context: vscode.ExtensionContext, checkSuccesfull: () => void)
{
	let rhetosServerFolderPath = vscode.workspace.rootPath + "/RhetosServerPath.txt";
	var fs = require('fs');
	if (fs.existsSync(rhetosServerFolderPath)) {
		var fs = require('fs');
		fs.readFile(rhetosServerFolderPath, 'utf8', function(err, data) {
			if (err){
				vscode.window.showErrorMessage("Error in reading the Rhetos server location.");
			}else{
				var rhetosServerLocation = data;
				if(fs.existsSync(rhetosServerLocation + "/bin/Plugins"))
				{
					checkSuccesfull();
				}else{
					vscode.window.showErrorMessage("The Rhetos server is not deployed yet. Please run DeployPackages.exe and then restart VSCode.");
				}
			}
			});
	}else{
		vscode.window.showErrorMessage("RhetosServerPath.txt file does not exist. Please create the file and then restart VSCode.");
	}
}

import {window, workspace, commands, Disposable, ExtensionContext, StatusBarAlignment, StatusBarItem, TextDocument} from 'vscode';

export function activate(context: vscode.ExtensionContext) {
	console.log("Rhetos extension is now activated.");
	activateLanguageServer(context);
}

export function deactivate() {
}