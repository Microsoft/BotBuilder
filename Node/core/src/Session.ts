// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK Github:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

import { Library, IRouteResult } from './bots/Library';
import { Dialog, IDialogResult, ResumeReason, IRecognizeDialogContext } from './dialogs/Dialog';
import { IRecognizeResult, IRecognizeContext } from './dialogs/IntentRecognizerSet';
import { ActionSet, IFindActionRouteContext, IActionRouteData } from './dialogs/ActionSet';
import { Message } from './Message';
import { DefaultLocalizer } from './DefaultLocalizer';
import { SessionLogger } from './SessionLogger';
import * as consts from './consts';
import * as utils from './utils';
import * as sprintf from 'sprintf-js';
import * as events from 'events';
import * as async from 'async';

export interface ISessionOptions {
    onSave: (done: (err: Error) => void) => void;
    onSend: (messages: IMessage[], done: (err: Error) => void) => void;
    library: Library;
    localizer: ILocalizer;
    logger: SessionLogger;
    middleware: ISessionMiddleware[];
    dialogId: string;
    dialogArgs?: any;
    autoBatchDelay?: number;
    dialogErrorMessage?: string|string[]|IMessage|IIsMessage;
    actions?: ActionSet;
}

export interface ISessionMiddleware {
    (session: Session, next: Function): void;
}

export interface IWatchableHandler {
    (context: IRecognizeContext, callback: (err: Error, value: any) => void): void;
}

export class Session extends events.EventEmitter {
    private msgSent = false;
    private _isReset = false;
    private lastSendTime = new Date().getTime();
    private batch: IMessage[] = [];
    private batchTimer: NodeJS.Timer;
    private batchStarted = false;
    private sendingBatch = false;
    private inMiddleware = false;
    private _locale:string = null;

    constructor(protected options: ISessionOptions) {
        super();
        this.library = options.library;
        this.localizer = options.localizer;
        this.logger = options.logger;
        if (typeof this.options.autoBatchDelay !== 'number') {
            this.options.autoBatchDelay = 250;  // 250ms delay
        }
    }

    public toRecognizeContext(): IRecognizeContext {
        return {
            message: this.message,
            userData: this.userData,
            conversationData: this.conversationData,
            privateConversationData: this.privateConversationData,
            dialogData: this.dialogData,
            localizer: this.localizer,
            logger: this.logger,
            dialogStack: () => { return this.dialogStack(); },
            preferredLocale: () => { return this.preferredLocale(); },
            gettext: (...args: any[]) => { return Session.prototype.gettext.call(this, args); }, 
            ngettext: (...args: any[]) => { return Session.prototype.ngettext.call(this, args); }, 
            locale: this.preferredLocale()
        };
    }

    public dispatch(sessionState: ISessionState, message: IMessage, done: Function): this {
        var index = 0;
        var session = this;
        var now = new Date().getTime();
        var middleware = this.options.middleware || [];
        var next = () => {
            var handler = index < middleware.length ? middleware[index] : null;
            if (handler) {
                index++;
                handler(session, next);
            } else {
                this.inMiddleware = false;
                this.sessionState.lastAccess = now; // Set after middleware runs so you can expire old sessions.
                done();
            }
        };

        // Make sure dialogData is properly initialized
        this.sessionState = sessionState || { callstack: [], lastAccess: now, version: 0.0 };
        var cur = this.curDialog();
        if (cur) {
            this.dialogData = cur.state;
        }

        // Dispatch message
        this.inMiddleware = true;
        this.message = <IMessage>(message || { text: '' });
        if (!this.message.type) {
            this.message.type = consts.messageType;
        }

        // Ensure localized prompts are loaded
        var locale = this.preferredLocale();
        this.localizer.load(locale, (err:Error) => {
            if (err) {
                    this.error(err);
            } else {
                next();
        }});
        return this;
    }

    public library: Library;
    public sessionState: ISessionState;
    public message: IMessage;
    public userData: any;
    public conversationData: any;
    public privateConversationData: any;
    public dialogData: any;
    public localizer: ILocalizer = null;
    public logger: SessionLogger = null;

    /** An error has occured. */
    public error(err: Error): this {
        // Log error
        var m = err.toString();
        err = err instanceof Error ? err : new Error(m);
        this.emit('error', err);
        this.logger.error(this.dialogStack(), err);

        // End conversation with a message
        if (this.options.dialogErrorMessage) {
            this.endConversation(this.options.dialogErrorMessage);
        } else {
            var locale = this.preferredLocale();
            this.endConversation(this.localizer.gettext(locale, 'default_error', consts.Library.system));
        }
        return this;
    }

    /** Gets/sets the users preferred locale. */
    public preferredLocale(locale?: string, callback?: ErrorCallback): string {
        if (locale) {
            this._locale = locale;
            if (this.userData) {
                this.userData[consts.Data.PreferredLocale] = locale;
            }
            if (this.localizer) {
                this.localizer.load(locale, callback);
            }
        } else if (!this._locale) {
            if (this.userData && this.userData[consts.Data.PreferredLocale]) {
                this._locale = this.userData[consts.Data.PreferredLocale];
            } else if (this.message && this.message.textLocale) {
                this._locale = this.message.textLocale;
            } else if (this.localizer) {
                this._locale = this.localizer.defaultLocale();
            }
        }        
        return this._locale;
    }

    /** Gets and formats a localized text string. */
    public gettext(messageid: string, ...args: any[]): string {
        return this.vgettext(this.curLibraryName(), messageid, args);
    }

    /** Gets and formats the singular/plural form of a localized text string. */
    public ngettext(messageid: string, messageid_plural: string, count: number): string {
        var tmpl: string;
        if (this.localizer && this.message) {
            tmpl = this.localizer.ngettext(this.preferredLocale(), messageid, messageid_plural, count, this.curLibraryName());
        } else if (count == 1) {
            tmpl = messageid;
        } else {
            tmpl = messageid_plural;
        }
        return sprintf.sprintf(tmpl, count);
    }
    
    /** Used to manually save the current session state. */
    public save(): this {
        this.logger.log(this.dialogStack(), 'Session.save()');            
        this.startBatch();
        return this;
    }

    /** Sends a message to the user. */
    public send(message: string|string[]|IMessage|IIsMessage, ...args: any[]): this {
        args.unshift(this.curLibraryName(), message);
        return Session.prototype.sendLocalized.apply(this, args);
    }

    /** Sends a message to a user using a specific localization namespace. */
    public sendLocalized(localizationNamespace: string, message: string|string[]|IMessage|IIsMessage, ...args: any[]): this {
        this.msgSent = true;
        if (message) {
            var m: IMessage;
            if (typeof message == 'string' || Array.isArray(message)) {
                m = this.createMessage(localizationNamespace, <string|string[]>message, args);
            } else if ((<IIsMessage>message).toMessage) {
                m = (<IIsMessage>message).toMessage();
            } else {
                m = <IMessage>message;
            }
            this.prepareMessage(m);
            this.batch.push(m);
            this.logger.log(this.dialogStack(), 'Session.send()');            
        }
        this.startBatch();
        return this;
    }

    /** Sends a typing indicator to the user. */
    public sendTyping(): this {
        this.msgSent = true;
        var m = <IMessage>{ type: 'typing' };
        this.prepareMessage(m);
        this.batch.push(m);
        this.logger.log(this.dialogStack(), 'Session.sendTyping()');            
        this.sendBatch();
        return this;        
    }

    /** Returns true if at least one message has been sent. */
    public messageSent(): boolean {
        return this.msgSent;
    }

    /** Begins a new dialog. */
    public beginDialog(id: string, args?: any): this {
        // Find dialog
        this.logger.log(this.dialogStack(), 'Session.beginDialog(' + id + ')');            
        var id = this.resolveDialogId(id);
        var dialog = this.findDialog(id);
        if (!dialog) {
            throw new Error('Dialog[' + id + '] not found.');
        }
        
        // Push dialog onto stack and start it
        // - Removed the call to save() here as an optimization. In the case of prompts
        //   we end up saving state twice, once here and again after they save off all of
        //   there params before sending the message.  This chnage does mean a dialog needs
        //   to either send a message or manually call session.save() when started but given
        //   most dialogs should always prompt the user is some way that seems reasonable and
        //   can save a number of intermediate calls to save.
        this.pushDialog({ id: id, state: {} });
        this.startBatch();
        dialog.begin(this, args);
        return this;
    }

    /** Replaces the existing dialog with a new one.  */
    public replaceDialog(id: string, args?: any): this {
        // Find dialog
        this.logger.log(this.dialogStack(), 'Session.replaceDialog(' + id + ')');            
        var id = this.resolveDialogId(id);
        var dialog = this.findDialog(id);
        if (!dialog) {
            throw new Error('Dialog[' + id + '] not found.');
        }
        
        // Update the stack and start dialog
        this.popDialog();
        this.pushDialog({ id: id, state: {} });
        this.startBatch();
        dialog.begin(this, args);
        return this;
    }

    /** Ends the conversation with the user. */
    public endConversation(message?: string|string[]|IMessage|IIsMessage, ...args: any[]): this {
        // Unpack message
        var m: IMessage;
        if (message) {
            if (typeof message == 'string' || Array.isArray(message)) {
                m = this.createMessage(this.curLibraryName(), <any>message, args);
            } else if ((<IIsMessage>message).toMessage) {
                m = (<IIsMessage>message).toMessage();
            } else {
                m = <IMessage>message;
            }
            this.msgSent = true;
            this.prepareMessage(m);
            this.batch.push(m);
        }

        // Clear private conversation data
        this.privateConversationData = {};
                
        // Clear stack and save.
        this.logger.log(this.dialogStack(), 'Session.endConversation()');            
        var ss = this.sessionState;
        ss.callstack = [];
        this.sendBatch();
        return this;
    }

    /** Ends the current dialog. */
    public endDialog(message?: string|string[]|IMessage|IIsMessage, ...args: any[]): this {
        // Check for result being passed
        if (typeof message === 'object' && (message.hasOwnProperty('response') || message.hasOwnProperty('resumed') || message.hasOwnProperty('error'))) {
            console.warn('Returning results via Session.endDialog() is deprecated. Use Session.endDialogWithResult() instead.')            
            return this.endDialogWithResult(<any>message);
        }

        // Validate callstack
        var cur = this.curDialog();
        if (cur) {
            // Unpack message
            var m: IMessage;
            if (message) {
                if (typeof message == 'string' || Array.isArray(message)) {
                    m = this.createMessage(this.curLibraryName(), <any>message, args);
                } else if ((<IIsMessage>message).toMessage) {
                    m = (<IIsMessage>message).toMessage();
                } else {
                    m = <IMessage>message;
                }
                this.msgSent = true;
                this.prepareMessage(m);
                this.batch.push(m);
            }
                    
            // Pop dialog off the stack and then resume parent.
            this.logger.log(this.dialogStack(), 'Session.endDialog()');            
            var childId = cur.id;
            cur = this.popDialog();
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, { resumed: ResumeReason.completed, response: true, childId: childId });
                } else {
                    // Bad dialog on the stack so just end it.
                    // - Because of the stack validation we should never actually get here.
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    }

    /** Ends the current dialog and returns a value to the caller. */
    public endDialogWithResult(result?: IDialogResult<any>): this {
        // Validate callstack
        var cur = this.curDialog();
        if (cur) {
            // Validate result
            result = result || <any>{};
            if (!result.hasOwnProperty('resumed')) {
                result.resumed = ResumeReason.completed;
            }
            result.childId = cur.id;
                    
            // Pop dialog off the stack and resume parent dlg.
            this.logger.log(this.dialogStack(), 'Session.endDialogWithResult()');            
            cur = this.popDialog();
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, result);
                } else {
                    // Bad dialog on the stack so just end it.
                    // - Because of the stack validation we should never actually get here.
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    }

    /** Cancels a specific dialog on teh stack and optionally replaces it with a new one. */
    public cancelDialog(dialogId: string|number, replaceWithId?: string, replaceWithArgs?: any): this {
        // Delete dialog(s)
        var childId = typeof dialogId === 'number' ? this.sessionState.callstack[<number>dialogId].id : <string>dialogId;
        var cur = this.deleteDialogs(dialogId);
        if (replaceWithId) {
            this.logger.log(this.dialogStack(), 'Session.cancelDialog(' + replaceWithId + ')');            
            var id = this.resolveDialogId(replaceWithId);
            var dialog = this.findDialog(id);
            this.pushDialog({ id: id, state: {} });
            this.startBatch();
            dialog.begin(this, replaceWithArgs);
        } else {
            this.logger.log(this.dialogStack(), 'Session.cancelDialog()');            
            this.startBatch();
            if (cur) {
                var dialog = this.findDialog(cur.id);
                if (dialog) {
                    dialog.dialogResumed(this, { resumed: ResumeReason.canceled, response: null, childId: childId });
                } else {
                    // Bad dialog on the stack so just end it.
                    // - Because of the stack validation we should never actually get here.
                    this.error(new Error("Can't resume missing parent dialog '" + cur.id + "'."));
                }
            }
        }
        return this;
    }

    /** Resets the dialog stack and starts a new dialog. */
    public reset(dialogId?: string, dialogArgs?: any): this {
        this.logger.log(this.dialogStack(), 'Session.reset()');            
        this._isReset = true;
        this.sessionState.callstack = [];
        if (!dialogId) {
            dialogId = this.options.dialogId;
            dialogArgs = this.options.dialogArgs;
        }
        this.beginDialog(dialogId, dialogArgs);
        return this;
    }

    /** Returns true if the session has been reset. */
    public isReset(): boolean {
        return this._isReset;
    }

    /** Manually triggers sending of the current auto-batch. */
    public sendBatch(callback?: (err: Error) => void): void {
        this.logger.log(this.dialogStack(), 'Session.sendBatch() sending ' + this.batch.length + ' message(s)');            
        if (this.sendingBatch) {
            return;
        }
        if (this.batchTimer) {
            clearTimeout(this.batchTimer);
            this.batchTimer = null;
        }
        this.batchTimer = null;
        var batch = this.batch;
        this.batch = [];
        this.batchStarted = false;
        this.sendingBatch = true;
        var cur = this.curDialog();
        if (cur) {
            cur.state = this.dialogData;
        }
        this.onSave((err) => {
            if (!err) {
                this.onSend(batch, (err) => {
                    this.onFinishBatch(() => {
                        if (this.batchStarted) {
                            this.startBatch();
                        }
                        if (callback) {
                            callback(err);
                        }
                    });
                });
            } else {
                this.onFinishBatch(() => {
                    if (callback) {
                        callback(err);
                    }
                });
            }
        });
    }


    //-------------------------------------------------------------------------
    // DialogStack Management
    //-------------------------------------------------------------------------

    /** Gets/sets the current dialog stack. A copy of the current stack will be returned to the caller. */
    public dialogStack(newStack?: IDialogState[]): IDialogState[] {
        var stack: IDialogState[];
        if (newStack) {
            // Update stack and dialog data.
            stack = this.sessionState.callstack = newStack;
            this.dialogData = stack.length > 0 ? stack[stack.length - 1].state : null;
        } else {
            // Copy over dialog data to stack.
            stack = this.sessionState.callstack || [];
            if (stack.length > 0) {
                stack[stack.length - 1].state = this.dialogData || {};
            }
        }
        return stack.slice(0);
    }

    /** Clears the current dialog stack. */
    public clearDialogStack(): this {
        this.sessionState.callstack = [];
        this.dialogData = null;
        return this;
    }
    
    /** Enumerates all a stacks dialog entries in either a forward or reverse direction. */
    static forEachDialogStackEntry(stack: IDialogState[], reverse: boolean, fn: (entry: IDialogState, index: number) => void): void {
        var step = reverse ? -1 : 1;
        var l = stack ? stack.length : 0;
        for (var i = step > 0 ? 0 : l - 1; i >= 0 && i < l; i += step) {
            fn(stack[i], i);
        }
    }

    /** Searches a dialog stack for a specific dialog, in either a forward or reverse direction, returning its index. */
    static findDialogStackEntry(stack: IDialogState[], dialogId: string, reverse = false): number {
        var step = reverse ? -1 : 1;
        var l = stack ? stack.length : 0;
        for (var i = step > 0 ? 0 : l - 1; i >= 0 && i < l; i += step) {
            if (stack[i].id === dialogId) {
                return i;
            }
        }
        return -1;
    } 

    /** Returns a stacks active dialog or null. */
    static activeDialogStackEntry(stack: IDialogState[]): IDialogState {
        return stack && stack.length > 0 ? stack[stack.length - 1] : null;
    }

    /** Pushes a new dialog onto a stack and returns it as the active dialog. */
    static pushDialogStackEntry(stack: IDialogState[], entry: IDialogState): IDialogState {
        if (!entry.state) {
            entry.state = {};
        }
        stack = stack || []
        stack.push(entry);
        return entry;
    }

    /** Pops the active dialog off a stack and returns the new one if the stack isn't empty.  */
    static popDialogStackEntry(stack: IDialogState[]): IDialogState {
        if (stack && stack.length > 0) {
            stack.pop();
        }
        return Session.activeDialogStackEntry(stack);
    }

    /** Deletes all dialog stack entries starting with the specified index and returns the new active dialog. */
    static pruneDialogStack(stack: IDialogState[], start: number): IDialogState {
        if (stack && stack.length > 0) {
            stack.splice(start);
        }
        return Session.activeDialogStackEntry(stack);
    }

    /** Ensures that all of the entries on a dialog stack reference valid dialogs within a library hierarchy. */
    static validateDialogStack(stack: IDialogState[], root: Library): boolean {
        let valid = true;
        Session.forEachDialogStackEntry(stack, false, (entry) => {
            var pair = entry.id.split(':');
            if (!root.findDialog(pair[0], pair[1])) {
                valid = false;
            }
        });
        return valid;
    }

    //-------------------------------------------------------------------------
    // Message Routing
    //-------------------------------------------------------------------------

    /** Dispatches handling of the current message to the active dialog stack entry. */
    public routeToActiveDialog(recognizeResult?: IRecognizeResult): void {
        var dialogStack = this.dialogStack();
        if (Session.validateDialogStack(dialogStack, this.library)) {
            var active = Session.activeDialogStackEntry(dialogStack);
            if (active) {
                var dialog = this.findDialog(active.id);
                dialog.replyReceived(this, recognizeResult);
            } else {
                this.beginDialog(this.options.dialogId, this.options.dialogArgs);
            }
        } else {
            this.error(new Error('Invalid Dialog Stack.'));
        }
    }

    //-----------------------------------------------------
    // Watch Statements
    //-----------------------------------------------------

    /** Enables/disables the watch statment for a given variable. */
    public watch(variable: string, enable = true): this {
        let name = variable.toLowerCase();
        if (!this.userData.hasOwnProperty(consts.Data.DebugWatches)) {
            this.userData[consts.Data.DebugWatches] = {};
        }
        if (watchableHandlers.hasOwnProperty(name)) {
            var entry = watchableHandlers[name];
            this.userData[consts.Data.DebugWatches][entry.name] = enable;
        } else {
            throw new Error("Invalid watch statement. '" + variable + "' isn't watchable");
        }
        return this;
    }

    /** Returns the list of enabled watch statements for the session. */
    public watchList(): string[] {
        var watches: string[] = []; 
        if (this.userData.hasOwnProperty(consts.Data.DebugWatches)) {
            for (let name in this.userData[consts.Data.DebugWatches]) {
                if (this.userData[consts.Data.DebugWatches][name]) {
                    watches.push(name);
                }
            }
        }
        return watches;
    }

    /** Adds or retrieves a watchable variable from the session. */
    static watchable(variable: string, handler?: IWatchableHandler): IWatchableHandler {
        if (handler) {
            watchableHandlers[variable.toLowerCase()] = { name: variable, handler: handler };
        } else {
            let entry = watchableHandlers[variable.toLowerCase()];
            if (entry) {
                handler = entry.handler;
            }
        }
        return handler;
    }

    /** Returns the list of watchable variables. */
    static watchableList(): string[] {
        let variables: string[] = [];
        for (let name in watchableHandlers) {
            if (watchableHandlers.hasOwnProperty(name)) {
                variables.push(watchableHandlers[name].name);
            }
        }
        return variables;
    }

    //-----------------------------------------------------
    // PRIVATE HELPERS
    //-----------------------------------------------------

    private onSave(cb: (err: Error) => void): void {
        this.options.onSave((err) => {
            if (err) {
                this.logger.error(this.dialogStack(), err);
                switch ((<any>err).code || '') {
                    case consts.Errors.EBADMSG:
                    case consts.Errors.EMSGSIZE:
                        // Something wrong with state so reset everything 
                        this.userData = {};
                        this.batch = [];
                        this.endConversation(this.options.dialogErrorMessage || 'Oops. Something went wrong and we need to start over.');
                        break;
                } 
            }
            cb(err);
        });
    }

    private onSend(batch: IMessage[], cb: (err: Error) => void): void {
        if (batch && batch.length > 0) {
            this.options.onSend(batch, (err) => {
                if (err) {
                    this.logger.error(this.dialogStack(), err);
                }
                cb(err);
            })
        } else {
            cb(null);
        }
    }

    private onFinishBatch(cb: Function): void {
        // Dump watchList
        var ctx = this.toRecognizeContext();
        async.each(this.watchList(), (variable, cb) => {
            let entry = watchableHandlers[variable.toLowerCase()];
            if (entry && entry.handler) {
                try {
                    entry.handler(ctx, (err, value) => {
                        if (!err) {
                            this.logger.dump(variable, value);
                        } 
                        cb(err);
                    });
                } catch (e) {
                    cb(e);
                }
            } else {
                cb(new Error("'" + variable + "' isn't watchable."));
            }
        }, (err) => {
            // Flush logs
            if (err) {
                this.logger.error(this.dialogStack(), err);
            }
            this.logger.flush((err) => {
                this.sendingBatch = false;
                if (err) {
                    console.error(err);
                }
                cb();
            });
        });
    }

    private startBatch(): void {
        this.batchStarted = true;
        if (!this.sendingBatch) {
            if (this.batchTimer) {
                clearTimeout(this.batchTimer);
            }
            this.batchTimer = setTimeout(() => {
                this.sendBatch();
            }, this.options.autoBatchDelay);
        }
    }

    private createMessage(localizationNamespace: string, text: string|string[], args?: any[]): IMessage {
        var message = new Message(this)
            .text(this.vgettext(localizationNamespace, Message.randomPrompt(text), args));
        return message.toMessage();
    }
    
    private prepareMessage(msg: IMessage): void {
        if (!msg.type) {
            msg.type = 'message';
        }
        if (!msg.address) {
            msg.address = this.message.address;
        }
        if (!msg.textLocale && this.message.textLocale) {
            msg.textLocale = this.message.textLocale;
        }
    }

 
    private vgettext(localizationNamespace: string, messageid: string, args?: any[]): string {
        var tmpl: string;
        if (this.localizer && this.message) {
            tmpl = this.localizer.gettext(this.preferredLocale(), messageid, localizationNamespace);
        } else {
            tmpl = messageid;
        }
        return args && args.length > 0 ? sprintf.vsprintf(tmpl, args) : tmpl;
    }

    /** Checks for any unsupported dialogs on the callstack. */
    private validateCallstack(): boolean {
        var ss = this.sessionState;
        for (var i = 0; i < ss.callstack.length; i++) {
            var id = ss.callstack[i].id;
            if (!this.findDialog(id)) {
                return false;
            }
        }
        return true;
    }

    private resolveDialogId(id: string) {
        return id.indexOf(':') >= 0 ? id : this.curLibraryName() + ':' + id;
    }

    private curLibraryName(): string {
        var cur = this.curDialog();
        return cur && !this.inMiddleware ? cur.id.split(':')[0] : this.library.name;
    }

    private findDialog(id: string): Dialog {
        var parts = id.split(':');
        return this.library.findDialog(parts[0] || this.library.name, parts[1]);
    }

    private pushDialog(ds: IDialogState): IDialogState {
        var ss = this.sessionState;
        var cur = this.curDialog();
        if (cur) {
            cur.state = this.dialogData || {};
        }
        ss.callstack.push(ds);
        this.dialogData = ds.state || {};
        return ds;
    }

    private popDialog(): IDialogState {
        var ss = this.sessionState;
        if (ss.callstack.length > 0) {
            ss.callstack.pop();
        }
        var cur = this.curDialog();
        this.dialogData = cur ? cur.state : null;
        return cur;
    }

    private deleteDialogs(dialogId?: string|number): IDialogState {
        var ss = this.sessionState;
        var index = -1;
        if (typeof dialogId === 'string') {
            for (var i = ss.callstack.length - 1; i >= 0; i--) {
                if (ss.callstack[i].id == dialogId) {
                    index = i;
                    break;
                }
            }
        } else {
            index = <number>dialogId;
        }
        if (index < 0 && index < ss.callstack.length) {
            throw new Error('Unable to cancel dialog. Dialog[' + dialogId + '] not found.');
        }
        ss.callstack.splice(index);
        var cur = this.curDialog();
        this.dialogData = cur ? cur.state : null;
        return cur;
    }

    private curDialog(): IDialogState {
        var cur: IDialogState;
        var ss = this.sessionState;
        if (ss.callstack.length > 0) {
            cur = ss.callstack[ss.callstack.length - 1];
        }
        return cur;
    }

    //-----------------------------------------------------
    // DEPRECATED METHODS
    //-----------------------------------------------------
    
    public getMessageReceived(): any {
        console.warn("Session.getMessageReceived() is deprecated. Use Session.message.sourceEvent instead.");
        return this.message.sourceEvent;
    }
}

// Initialize default list of watchable variables.
let watchableHandlers: { [name: string]: { name: string; handler: IWatchableHandler; }; } = {
    'userdata': { name: 'userData', handler: (ctx, cb) => cb(null, ctx.userData) },
    'conversationdata': { name: 'conversationData', handler: (ctx, cb) => cb(null, ctx.conversationData) },
    'privateconversationdata': { name: 'privateConversationData', handler: (ctx, cb) => cb(null, ctx.privateConversationData) },
    'dialogdata': { name: 'dialogData', handler: (ctx, cb) => cb(null, ctx.dialogData) },
    'dialogstack': { name: 'dialogStack', handler: (ctx, cb) => cb(null, ctx.dialogStack()) },
    'preferredlocale': { name: 'preferredLocale', handler: (ctx, cb) => cb(null, ctx.preferredLocale()) },
    'libraryname': { name: 'libraryName', handler: (ctx, cb) => cb(null, ctx.libraryName) }
};

