/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path'
import { execSync } from "child_process";

export class ApiCompat {
    private apiCompatPath: string;

    public constructor() {
        this.apiCompatPath = join(__dirname, '..', 'ApiCompat', 'Microsoft.DotNet.ApiCompat.exe');
    }

    public getVersion = (): string => {
        return this.runCommand('--version').toString();
    }

    public compare = (contracts: string, implementation: string, optionalParameters?: string): string => {
        const options = optionalParameters != undefined ? optionalParameters : '';
        const command = `"${ contracts }" --impl-dirs "${ implementation }" ${ options }`;
        return this.runCommand(command);
    }

    private runCommand = (command: string): string => {
        return execSync(`"${this.apiCompatPath}" ` + command).toString();
    }
}
