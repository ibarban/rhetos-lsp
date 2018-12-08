"use strict";

import * as vscode from "vscode";
import * as languageClient from "vscode-languageclient";
import * as path from "path";
import * as fs from "fs";

const languageServerExe = [
	"server/RhetosLanguageServer.exe",
	"../../RhetosLanguageServer/bin/Debug/RhetosLanguageServer.exe",
]

const rhetosServerConfigFileName = "RhetosServerPath.config";

function activateLanguageServer(context: vscode.ExtensionContext) {
	let serverModule: string = null;
	for (let p of languageServerExe) {
		p = context.asAbsolutePath(p);

		if (fs.existsSync(p)) {
			serverModule = p;
			break;
		}
	}

	checkRhetosServerSetup(context, (rhetosServerLocation: string)=>{
		if (!serverModule) throw new URIError("Cannot find the language server module.");
		let workPath = path.dirname(serverModule);

		let serverOptions: languageClient.ServerOptions = {
			run: { command: serverModule, args: ["--rhetos-server-path", rhetosServerLocation], options: { cwd: workPath } },
			debug: { command: serverModule, args: ["--debug", "--rhetos-server-path", rhetosServerLocation], options: { cwd: workPath } }
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

export function checkRhetosServerSetup(context: vscode.ExtensionContext, checkSuccesfull: (rhetosServerLocation: string) => void)
{
	let rhetosServerFolderPath = vscode.workspace.rootPath + "/" + rhetosServerConfigFileName;
	var fs = require('fs');
	if (fs.existsSync(rhetosServerFolderPath)) {
		var fs = require('fs');
		fs.readFile(rhetosServerFolderPath, 'utf8', function(err, data) {
			if (err) {
				vscode.window.showErrorMessage("Error in reading the Rhetos server location.");
			} else {
				var rhetosServerLocation = data;
				if (fs.existsSync(rhetosServerLocation + "/bin/Plugins")) {
					checkSuccesfull(rhetosServerLocation);
				} else {
					vscode.window.showErrorMessage("The Rhetos server is not deployed yet. Please run DeployPackages.exe and then restart VSCode.");
				}
			}
		});
	} else {
		vscode.window.showErrorMessage(rhetosServerConfigFileName + " file does not exist. Please create the file and then restart VSCode.");
	}
}

import {window, workspace, commands, Disposable, ExtensionContext, StatusBarAlignment, StatusBarItem, TextDocument} from 'vscode';

export function activate(context: vscode.ExtensionContext) {
	console.log("Rhetos extension is now activated.");
	activateLanguageServer(context);
}

export function deactivate() {
}