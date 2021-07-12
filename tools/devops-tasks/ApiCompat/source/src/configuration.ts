/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path'
import { getInput, getBoolInput } from "azure-pipelines-task-lib";
import { existsSync } from 'fs';

export class Configuration {
    public contractsFolder: string;
    public contract: string;
    public contractList: string;
    public implementationFolder: string;
    public failOnIssue: boolean;
    public resolveFx: boolean;
    public warnOnIncorrectVersion: boolean;
    public warnOnMissingAssemblies: boolean;
    public generateLog: boolean;
    public outputFileName: string;
    public outputFolder: string;
    public useBaseline: boolean;
    public baselineFile: string = '';

    public constructor() {
        this.contractsFolder = this.validatePath('contractsRootFolder');
        this.contract = getInput('contractsFileName');
        this.contractList = this.getContractsName();
        this.implementationFolder = this.validatePath('implFolder');
        this.failOnIssue = getBoolInput('failOnIssue');
        this.resolveFx = getBoolInput('resolveFx');
        this.warnOnIncorrectVersion = getBoolInput('warnOnIncorrectVersion');
        this.warnOnMissingAssemblies = getBoolInput('warnOnMissingAssemblies');
        this.generateLog = getBoolInput('generateLog');
        this.outputFileName = getInput('outputFilename');
        this.outputFolder = getInput('outputFolder');
        
        this.useBaseline = getBoolInput('useBaseline');
        if (this.useBaseline) {
            this.baselineFile = this.validatePath('baselineFile');
        }
    }

    private validatePath = (inputName: string): string => {
        const path = getInput(inputName);
        
        if (!existsSync(path)) {
            throw new Error(`The file or directory "${ path }" specified in "${ inputName }" does not exist.`);
        }
    
        return path;
    }

    private getContractsName = (): string => {
        const filesName: string[] = [];
    
        getInput('contractsFileName').split(' ').forEach(file => {
            const fullFilePath: string = join(this.validatePath('contractsRootFolder'), file);
            if (existsSync(fullFilePath)) {
                filesName.push(fullFilePath);
            }
        });
    
        if (filesName.length == 0) {
            throw new Error('The specified contracts were not found.');
        }
    
        return filesName.join(',');
    }
}
