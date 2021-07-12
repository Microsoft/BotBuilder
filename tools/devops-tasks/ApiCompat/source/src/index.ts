/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { setResult, TaskResult } from 'azure-pipelines-task-lib';
import { ApiCompat } from './ApiCompat'
import { Configuration } from './Configuration'
import { Result } from './Result';
import { existsSync, writeFileSync, mkdirSync } from 'fs';
import { EOL } from 'os';
import { join } from 'path'

const getOptionalParameters = (configuration: Configuration): string => {
    let command = configuration.resolveFx ? ' --resolve-fx' : '';
    command += configuration.warnOnIncorrectVersion ? ' --warn-on-incorrect-version' : '';
    command += configuration.warnOnMissingAssemblies ? ' --warn-on-missing-assemblies' : '';
    command += configuration.useBaseline ? ` --baseline "${ configuration.baselineFile }"` : '';

    return command;
}

const resultText = (result: Result): string => {
    return result.issuesCount != 0
        ? `There were ${ result.issuesCount } differences between the assemblies`
        : 'No differences were found between the assemblies';
}

const writeResult = (result: Result, configuration: Configuration): void => {
    const fileName: string = configuration.outputFileName;
    const directory: string = configuration.outputFolder;
    
    const resultText: string = result.issuesCount === 0
        ? `:heavy_check_mark: No Binary Compatibility issues for **${ configuration.contract }**`
        : result.getFormattedResult()
        
    if (!existsSync(directory)) {
        mkdirSync(directory, { recursive: true });
    }
    
    writeFileSync(`${join(directory, fileName)}`, resultText );
}

const run = (): void => {
    try {
        const apiCompat: ApiCompat = new ApiCompat();
        const configuration: Configuration = new Configuration();
        
        console.log(apiCompat.getVersion());

        const optionalParameters = getOptionalParameters(configuration);
        const comparisonResult = apiCompat.compare(
            configuration.contractList,
            configuration.implementationFolder,
            optionalParameters);
        const result: Result = new Result(comparisonResult);
        
        console.log(comparisonResult.concat(EOL));

        if (configuration.generateLog) {
            writeResult(result, configuration);
        }
        
        setResult(result.checkResult(configuration), resultText(result));
    } catch (error) {
        setResult(TaskResult.Failed, error);
    }
}

run();
