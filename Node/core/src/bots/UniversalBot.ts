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

import { Library, systemLib, IRouteResult } from './Library';
import { IDialogWaterfallStep } from '../dialogs/SimpleDialog';
import { Session, ISessionMiddleware } from '../Session';
import { DefaultLocalizer } from '../DefaultLocalizer';
import { IBotStorage, IBotStorageContext, IBotStorageData, MemoryBotStorage } from '../storage/BotStorage';
import { IIntentRecognizerResult } from '../dialogs/IntentRecognizerSet';
import { SessionLogger } from '../SessionLogger';
import { RemoteSessionLogger } from '../RemoteSessionLogger';
import * as consts from '../consts';
import * as utils from '../utils';
import * as async from 'async';

export interface IUniversalBotSettings {
    defaultDialogId?: string;
    defaultDialogArgs?: any;
    localizerSettings?: IDefaultLocalizerSettings;    
    lookupUser?: ILookupUser;
    processLimit?: number;
    autoBatchDelay?: number;
    storage?: IBotStorage;
    persistUserData?: boolean;
    persistConversationData?: boolean;
    dialogErrorMessage?: string|string[]|IMessage|IIsMessage;
}

export interface IConnector {
    onEvent(handler: (events: IEvent[], cb?: (err: Error) => void) => void): void;
    onInvoke?(handler: (event: IEvent, cb?: (err: Error, body: any, status?: number) => void) => void): void;
    send(messages: IMessage[], cb: (err: Error) => void): void;
    startConversation(address: IAddress, cb: (err: Error, address?: IAddress) => void): void;
}
interface IConnectorMap {
    [channel: string]: IConnector;    
}

export interface IMiddlewareMap {
    receive?: IEventMiddleware|IEventMiddleware[];
    send?: IEventMiddleware|IEventMiddleware[];
    botbuilder?: ISessionMiddleware|ISessionMiddleware[];
}

export interface IEventMiddleware {
    (event: IEvent, next: Function): void;
}

export interface ILookupUser {
    (address: IAddress, done: (err: Error, user: IIdentity) => void): void;
}

export interface IDisambiguateRouteHandler {
    (session: Session, routes: IRouteResult[]): void;
}


export class UniversalBot extends Library {
    private settings = <IUniversalBotSettings>{ 
        processLimit: 4, 
        persistUserData: true, 
        persistConversationData: false 
    };
    private connectors = <IConnectorMap>{}; 
    private mwReceive = <IEventMiddleware[]>[];
    private mwSend = <IEventMiddleware[]>[];
    private mwSession = <ISessionMiddleware[]>[]; 
    private localizer: DefaultLocalizer;
    private _onDisambiguateRoute: IDisambiguateRouteHandler;
    
    constructor(connector: IConnector, settings?: IUniversalBotSettings);
    constructor(connector: IConnector, defaultDialog?: IDialogWaterfallStep|IDialogWaterfallStep[], libraryName?: string);
    constructor(connector?: IConnector, defaultDialog?: any, libraryName?: string) {
        super(libraryName || consts.Library.default);
        this.localePath('./locale/');
        this.library(systemLib);
        if (defaultDialog) {
            // Check for legacy settings passed in
            if (typeof defaultDialog === 'function' || Array.isArray(defaultDialog)) {
                this.dialog('/', defaultDialog);
            } else {
                var settings = <IUniversalBotSettings>defaultDialog;
                for (var name in settings) {
                    if (settings.hasOwnProperty(name)) {
                        this.set(name, (<any>settings)[name]);
                    }
                }
            }
        }
        if (connector) {
            this.connector(consts.defaultConnector, connector);
        }
    }

    public clone(copyTo?: UniversalBot, newName?: string): UniversalBot {
        var obj = copyTo || new UniversalBot(null, null, newName || this.name);
        for (var name in this.settings) {
            if (this.settings.hasOwnProperty(name)) {
                this.set(name, (<any>this.settings)[name]);
            }
        }
        for (var channel in this.connectors) {
            obj.connector(channel, this.connectors[channel]);
        }
        obj.mwReceive = this.mwReceive.slice(0);
        obj.mwSession = this.mwSession.slice(0);
        obj.mwSend = this.mwSend.slice(0);
        // obj.localizer is automatically created based on settings
        return <UniversalBot>super.clone(obj);
    }
    
    //-------------------------------------------------------------------------
    // Settings
    //-------------------------------------------------------------------------
    
    public set(name: string, value: any): this {
        (<any>this.settings)[name] = value;
        if (value && name === 'localizerSettings') {
            var settings = <IDefaultLocalizerSettings>value;
            if (settings.botLocalePath) {
                this.localePath(settings.botLocalePath);
            }
        }
        return this;
    }
    
    public get(name: string): any {
        return (<any>this.settings)[name];
    }
    
    //-------------------------------------------------------------------------
    // Connectors
    //-------------------------------------------------------------------------
    
    public connector(channelId: string, connector?: IConnector): IConnector {
        var c: IConnector;
        if (connector) {
            // Bind to connector.
            this.connectors[channelId || consts.defaultConnector] = c = connector;
            c.onEvent((events, cb) => this.receive(events, cb));

            // Optionally use connector for storage.
            var asStorage: IBotStorage = <any>connector;
            if (!this.settings.storage && 
                typeof asStorage.getData === 'function' &&
                typeof asStorage.saveData === 'function') {
                this.settings.storage = asStorage;
            }
        } else if (this.connectors.hasOwnProperty(channelId)) {
            c = this.connectors[channelId];
        } else if (this.connectors.hasOwnProperty(consts.defaultConnector)) {
            c = this.connectors[consts.defaultConnector];
        }
        return c;
    }

    //-------------------------------------------------------------------------
    // Middleware
    //-------------------------------------------------------------------------
    
    public use(...args: IMiddlewareMap[]): this {
        args.forEach((mw) => {
            var added = 0;
            if (mw.receive) {
                Array.prototype.push.apply(this.mwReceive, Array.isArray(mw.receive) ? mw.receive : [mw.receive]);
                added++;
            }
            if (mw.send) {
                Array.prototype.push.apply(this.mwSend, Array.isArray(mw.send) ? mw.send : [mw.send]);
                added++;
            }
            if (mw.botbuilder) {
                Array.prototype.push.apply(this.mwSession, Array.isArray(mw.botbuilder) ? mw.botbuilder : [mw.botbuilder]);
                added++;
            }
            if (added < 1) {
                console.warn('UniversalBot.use: no compatible middleware hook found to install.')
            }
        });
        return this;    
    }
    
    //-------------------------------------------------------------------------
    // Messaging
    //-------------------------------------------------------------------------
    
    public receive(events: IEvent|IEvent[], done?: (err: Error) => void): void {
        var list: IEvent[] = Array.isArray(events) ? events : [events]; 
        async.eachLimit(list, this.settings.processLimit, (message: IMessage, cb: (err: Error) => void) => {
            message.agent = consts.agent;
            message.type = message.type || consts.messageType;
            this.lookupUser(message.address, (user) => {
                if (user) {
                    message.user = user;
                }
                this.emit('receive', message);
                this.eventMiddleware(message, this.mwReceive, () => {
                    if (this.isMessage(message)) {
                        this.emit('incoming', message);
                        var userId = message.user.id;
                        var conversationId = message.address.conversation ? message.address.conversation.id : null;
                        var storageCtx: IBotStorageContext = { 
                            userId: userId, 
                            conversationId: conversationId, 
                            address: message.address,
                            persistUserData: this.settings.persistUserData,
                            persistConversationData: this.settings.persistConversationData 
                        };
                        this.dispatch(storageCtx, message, this.settings.defaultDialogId || '/', this.settings.defaultDialogArgs, cb);
                    } else {
                        // Dispatch incoming activity
                        this.emit(message.type, message);
                        cb(null);
                    }
                }, cb);
            }, cb);
        }, this.errorLogger(done));
    }
 
    public beginDialog(address: IAddress, dialogId: string, dialogArgs?: any, done?: (err: Error) => void): void {
        this.lookupUser(address, (user) => {
            var msg = <IMessage>{
                type: consts.messageType,
                agent: consts.agent,
                source: address.channelId,
                sourceEvent: {},
                address: utils.clone(address),
                text: '',
                user: user
            };
            this.ensureConversation(msg.address, (adr) => {
                msg.address = adr;
                var conversationId = msg.address.conversation ? msg.address.conversation.id : null;
                var storageCtx: IBotStorageContext = { 
                    userId: msg.user.id, 
                    conversationId: conversationId,
                    address: msg.address,
                    persistUserData: this.settings.persistUserData,
                    persistConversationData: this.settings.persistConversationData 
                };
                this.dispatch(storageCtx, msg, dialogId, dialogArgs, this.errorLogger(done), true);
            }, this.errorLogger(done));
        }, this.errorLogger(done));
    }
    
    public send(messages: IIsMessage|IMessage|IMessage[], done?: (err: Error) => void): void {
        var list: IMessage[];
        if (Array.isArray(messages)) {
            list = messages;
        } else if ((<IIsMessage>messages).toMessage) {
            list = [(<IIsMessage>messages).toMessage()];
        } else {
            list = [<IMessage>messages];
        }
        async.eachLimit(list, this.settings.processLimit, (message, cb) => {
            this.ensureConversation(message.address, (adr) => {
                message.address = adr;
                this.emit('send', message);
                this.eventMiddleware(message, this.mwSend, () => {
                    this.emit('outgoing', message);
                    cb(null);
                }, cb);
            }, cb);
        }, this.errorLogger((err) => {
            if (!err && list.length > 0) {
                this.tryCatch(() => {
                    // All messages should be targeted at the same channel.
                    var channelId = list[0].address.channelId;
                    var connector = this.connector(channelId);
                    if (!connector) {
                        throw new Error("Invalid channelId='" + channelId + "'");
                    }
                    connector.send(list, this.errorLogger(done));
                }, this.errorLogger(done));
            } else if (done) {
                done(err);
            }
        }));
    }

    public isInConversation(address: IAddress, cb: (err: Error, lastAccess: Date) => void): void {
        this.lookupUser(address, (user) => {
            var conversationId = address.conversation ? address.conversation.id : null;
            var storageCtx: IBotStorageContext = { 
                userId: user.id, 
                conversationId: conversationId, 
                address: address,
                persistUserData: false,
                persistConversationData: false 
            };
            this.getStorageData(storageCtx, (data) => {
                var lastAccess: Date;
                if (data && data.privateConversationData && data.privateConversationData.hasOwnProperty(consts.Data.SessionState)) {
                    var ss: ISessionState = data.privateConversationData[consts.Data.SessionState];
                    if (ss && ss.lastAccess) {
                        lastAccess = new Date(ss.lastAccess);
                    }
                }
                cb(null, lastAccess);
            }, this.errorLogger(<any>cb));
        }, this.errorLogger(<any>cb));
    }

    /** Lets a developer override the bots default route disambiguation logic. */
    public onDisambiguateRoute(handler: IDisambiguateRouteHandler): void {
        this._onDisambiguateRoute = handler;
    }

    //-------------------------------------------------------------------------
    // Session
    //-------------------------------------------------------------------------
    
    /** Loads a session object for an arbitrary address. */
    public loadSession(address: IAddress, done: (err: Error, session: Session) => void): void {
        this.lookupUser(address, (user) => {
            var msg = <IMessage>{
                type: consts.messageType,
                agent: consts.agent,
                source: address.channelId,
                sourceEvent: {},
                address: utils.clone(address),
                text: '',
                user: user
            };
            this.ensureConversation(msg.address, (adr) => {
                msg.address = adr;
                var conversationId = msg.address.conversation ? msg.address.conversation.id : null;
                var storageCtx: IBotStorageContext = { 
                    userId: msg.user.id, 
                    conversationId: conversationId,
                    address: msg.address,
                    persistUserData: this.settings.persistUserData,
                    persistConversationData: this.settings.persistConversationData 
                };
                this.createSession(storageCtx, msg, this.settings.defaultDialogId || '/', this.settings.defaultDialogArgs, done);
            }, this.errorLogger(<any>done));
        }, this.errorLogger(<any>done));
    }

    //-------------------------------------------------------------------------
    // Helpers
    //-------------------------------------------------------------------------
    
    private dispatch(storageCtx: IBotStorageContext, message: IMessage, dialogId: string, dialogArgs: any, done: (err: Error) => void, newStack = false): void {
        // --------------------------------------------------------------------
        // Theory of Operation
        // --------------------------------------------------------------------
        // The dispatch() function is called for both reactive & pro-active 
        // messages and while they generally work the same there are some 
        // differences worth noting.
        //
        // REACTIVE:
        // * The passed in storageKey will have the normalized userId and the
        //   conversationId for the incoming message. These are used as keys to
        //   load the persisted userData and conversationData objects.
        // * After loading data from storage we create a new Session object and
        //   dispatch the incoming message to the active dialog.
        // * As part of the normal dialog flow the session will call onSave() 1 
        //   or more times before each call to onSend().  Anytime onSave() is 
        //   called we'll save the current userData & conversationData objects
        //   to storage.
        //
        // PROACTIVE:
        // * Proactive follows essentially the same flow but the difference is 
        //   the passed in storageKey will only have a userId and not a 
        //   conversationId as this is a new conversation.  This will cause use
        //   to load userData but conversationData will be set to {}.
        // * When onSave() is called for a proactive message we don't know the
        //   conversationId yet so we can't actually save anything. The first
        //   call to this.send() results in a conversationId being assigned and
        //   that's the point at which we can actually save state. So we'll update
        //   the storageKey with the new conversationId and then manually trigger
        //   saving the userData & conversationData to storage.
        // * After the first call to onSend() for the conversation everything 
        //   follows the same flow as for reactive messages.
        this.createSession(storageCtx, message, dialogId, dialogArgs, (err, session) => {
            // Dispatch message
            if (!err) {
                this.emit('routing', session);
                this.routeMessage(session, done);
            } else {
                done(err);
            }
        }, newStack);
    }

    private createSession(storageCtx: IBotStorageContext, message: IMessage, dialogId: string, dialogArgs: any, done: (err: Error, session: Session) => void, newStack = false): void {
        var loadedData: IBotStorageData;
        this.getStorageData(storageCtx, (data) => {
            // Create localizer on first access
            if (!this.localizer) {
                var defaultLocale = this.settings.localizerSettings ? this.settings.localizerSettings.defaultLocale : null;
                this.localizer = new DefaultLocalizer(this, defaultLocale);
            }

            // Create logger
            let logger: SessionLogger;
            if (message.source == consts.emulatorChannel) {
                logger = new RemoteSessionLogger(this.connector(consts.emulatorChannel), message.address, message.address);
            } else if (data.privateConversationData && data.privateConversationData.hasOwnProperty(consts.Data.DebugAddress)) {
                var debugAddress = data.privateConversationData[consts.Data.DebugAddress];
                logger = new RemoteSessionLogger(this.connector(consts.emulatorChannel), debugAddress, message.address);
            } else {
                logger = new SessionLogger();
            }

            // Initialize session
            var session = new Session({
                localizer: this.localizer,
                logger: logger,
                autoBatchDelay: this.settings.autoBatchDelay,
                library: this,
                //actions: this.actions,
                middleware: this.mwSession,
                dialogId: dialogId,
                dialogArgs: dialogArgs,
                dialogErrorMessage: this.settings.dialogErrorMessage,
                onSave: (cb) => {
                    var finish = this.errorLogger(cb);
                    loadedData.userData = utils.clone(session.userData);
                    loadedData.conversationData = utils.clone(session.conversationData);
                    loadedData.privateConversationData = utils.clone(session.privateConversationData);
                    loadedData.privateConversationData[consts.Data.SessionState] = session.sessionState;
                    this.saveStorageData(storageCtx, loadedData, finish, finish);
                },
                onSend: (messages, cb) => {
                    this.send(messages, cb);
                }
            });
            session.on('error', (err: Error) => this.emitError(err));
            
            // Initialize session data
            var sessionState: ISessionState;
            session.userData = data.userData || {};
            session.conversationData = data.conversationData || {};
            session.privateConversationData = data.privateConversationData || {};
            if (session.privateConversationData.hasOwnProperty(consts.Data.SessionState)) {
                sessionState = newStack ? null : session.privateConversationData[consts.Data.SessionState];
                delete session.privateConversationData[consts.Data.SessionState];
            }
            loadedData = data;  // We'll clone it when saving data later
            
            // Dispatch message
            session.dispatch(sessionState, message, () => done(null, session));
        }, <any>done);
    }

    private routeMessage(session: Session, done: (err: Error) => void): void {
        // Log start of routing
        var entry = 'UniversalBot("' + this.name + '") routing ';
        if (session.message.text) {
            entry += '"' + session.message.text + '"';
        } else if (session.message.attachments && session.message.attachments.length > 0) {
            entry += session.message.attachments.length + ' attachment(s)';
        } else {
            entry += '<null>';
        }
        entry += ' from "' + session.message.source + '"';
        session.logger.log(null, entry);
        
        // Run the root libraries recognizers
        var context = session.toRecognizeContext();
        this.recognize(context, (err, topIntent) => {
            // Check for forwarded intent
            if (session.message.entities) {
                session.message.entities.forEach((entity) => {
                    if (entity.type === consts.intentEntityType && 
                        (<IIntentRecognizerResult>entity).score > topIntent.score) {
                        topIntent = entity;
                    } 
                });
            }

            // This intent will be automatically inherited by child libraries
            // that don't implement their own recognizers.
            // - We're passing along the library name to avoid running our own
            //   recognizer twice.
            context.intent = topIntent;
            context.libraryName = this.name;

            // Federate across all libraries to find the best route to trigger. 
            var results = Library.addRouteResult({ score: 0.0, libraryName: this.name });
            async.each(this.libraryList(), (lib, cb) => {
                lib.findRoutes(context, (err, routes) => {
                    if (!err && routes) {
                        routes.forEach((r) => results = Library.addRouteResult(r, results));
                    }
                    cb(err);
                });
            }, (err) => {
                if (!err) {
                    // Find disambiguation handler to use
                    var disambiguateRoute: IDisambiguateRouteHandler = (session, routes) => {
                        var route = Library.bestRouteResult(results, session.dialogStack(), this.name);
                        if (route) {
                            this.library(route.libraryName).selectRoute(session, route);
                        } else {
                            // Just let the active dialog process the message
                            session.routeToActiveDialog();
                        }
                    };
                    if (this._onDisambiguateRoute) {
                        disambiguateRoute = this._onDisambiguateRoute;
                    }

                    // Select best route and dispatch message.
                    disambiguateRoute(session, results);
                    done(null);
                } else {
                    // Let the session process the error
                    session.error(err);
                    done(err);
                }
            });
        });
    }

    private eventMiddleware(event: IEvent, middleware: IEventMiddleware[], done: Function, error?: (err: Error) => void): void {
        var i = -1;
        var _that = this;
        function next() {
            if (++i < middleware.length) {
                _that.tryCatch(() => {
                    middleware[i](event, next);
                }, () => next());
            } else {
                _that.tryCatch(() => done(), error);
            }
        }
        next();
    }

    private isMessage(message: IMessage): boolean {
        return (message && message.type && message.type.toLowerCase() == consts.messageType);
    }
    
    private ensureConversation(address: IAddress, done: (adr: IAddress) => void, error?: (err: Error) => void): void {
        this.tryCatch(() => {
            if (!address.conversation) {
                var connector = this.connector(address.channelId);
                if (!connector) {
                    throw new Error("Invalid channelId='" + address.channelId + "'");
                }
                connector.startConversation(address, (err, adr) => {
                    if (!err) {
                        this.tryCatch(() => done(adr), error);
                    } else if (error) {
                        error(err);
                    }
                });
            } else {
                this.tryCatch(() => done(address), error);
            }
        }, error);
    }
    
    private lookupUser(address: IAddress, done: (user: IIdentity) => void, error?: (err: Error) => void): void {
        this.tryCatch(() => {
            this.emit('lookupUser', address);
            if (this.settings.lookupUser) {
                this.settings.lookupUser(address, (err, user) => {
                    if (!err) {
                        this.tryCatch(() => done(user || address.user), error);
                    } else if (error) {
                        error(err);
                    }
                });
            } else {
                this.tryCatch(() => done(address.user), error);
            }
        }, error);
    }
    
    private getStorageData(storageCtx: IBotStorageContext, done: (data: IBotStorageData) => void, error?: (err: Error) => void): void {
        this.tryCatch(() => {
            this.emit('getStorageData', storageCtx);
            var storage = this.getStorage();
            storage.getData(storageCtx, (err, data) => {
                if (!err) {
                    this.tryCatch(() => done(data || {}), error);
                } else if (error) {
                    error(err);
                } 
            });  
        }, error);
    }
    
    private saveStorageData(storageCtx: IBotStorageContext, data: IBotStorageData, done?: Function, error?: (err: Error) => void): void {
        this.tryCatch(() => {
            this.emit('saveStorageData', storageCtx);
            var storage = this.getStorage();
            storage.saveData(storageCtx, data, (err) => {
                if (!err) {
                    if (done) {
                        this.tryCatch(() => done(), error);
                    }
                } else if (error) {
                    error(err);
                } 
            });  
        }, error);
    }

    private getStorage(): IBotStorage {
        if (!this.settings.storage) {
            this.settings.storage = new MemoryBotStorage();
        }
        return this.settings.storage;
    }
    
    private tryCatch(fn: Function, error?: (err?: Error) => void): void {
        try {
            fn();
        } catch (e) {
            try {
                if (error) {
                    error(e);
                }
            } catch (e2) {
                this.emitError(e2);
            }
        }
    }

    private errorLogger(done?: (err: Error) => void): (err: Error) => void {
        return (err: Error) => {
            if (err) {
                this.emitError(err);
            }
            if (done) {
                done(err);
                done = null;
            }
        };
    }
     
    private emitError(err: Error): void {
        var m = err.toString();
        var e = err instanceof Error ? err : new Error(m);
        if (this.listenerCount('error') > 0) {
            this.emit('error', e);
        } else {
            console.error(e.stack);
        }
    }
}