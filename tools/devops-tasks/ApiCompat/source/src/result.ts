/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { EOL } from 'os';
import { Configuration } from './Configuration';
import { TaskResult } from "azure-pipelines-task-lib";

export class Result {
    public assemblyName: string;
    public issues: string;
    public issuesCount: number;

    public constructor(result: string) {
        let resultLines: string[] = result.split(EOL);
        this.assemblyName = this.getAssemblyName(resultLines[0]);
        resultLines = resultLines.slice(1, -2);
        this.issues = resultLines.join(EOL);
        this.issuesCount = resultLines.length;
    }

    public checkResult = (configuration: Configuration): TaskResult => {
        if (this.issuesCount  === 0) {
            return TaskResult.Succeeded;
        } else if (configuration.failOnIssue) {
            return TaskResult.Failed;
        } else {
            return TaskResult.SucceededWithIssues;
        }
    }

    public getFormattedResult = (): string => {
        const icon: string = this.issuesCount == 0 ? ':heavy_check_mark:' : ':x:';
        const codeFence = '```';
        const title = `${ icon } ${ this.issuesCount } Binary Compatibility issues for **${ this.assemblyName }**`;
        const body = `<details>${ EOL + EOL + codeFence + EOL + this.issues + EOL + codeFence + EOL + EOL }</details>${ EOL }`;
    
        return (title.concat(EOL).concat(body));
    }

    private getAssemblyName = (line: string): string => {
        return line.replace('Compat issues with assembly ', '').replace(':', '');
    }
}
