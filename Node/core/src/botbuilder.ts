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

import * as consts from './consts';
import * as utils from './utils';
import * as Session from './Session';
import * as Message from './Message';
import * as Dialog from './dialogs/Dialog';
import * as DialogAction from './dialogs/DialogAction';
import * as Prompts from './dialogs/Prompts';
import * as SimpleDialog from './dialogs/SimpleDialog';
import * as EntityRecognizer from './dialogs/EntityRecognizer';
import * as Library from './bots/Library';
import * as UniversalBot from './bots/UniversalBot';
import * as ChatConnector from './bots/ChatConnector';
import * as ConsoleConnector from './bots/ConsoleConnector';
import * as BotStorage from './storage/BotStorage';
import * as CardAction from './cards/CardAction';
import * as HeroCard from './cards/HeroCard';
import * as CardImage from './cards/CardImage';
import * as ReceiptCard from './cards/ReceiptCard';
import * as SigninCard from './cards/SigninCard';
import * as ThumbnailCard from './cards/ThumbnailCard';
import * as VideoCard from './cards/VideoCard';
import * as AudioCard from './cards/AudioCard';
import * as AnimationCard from './cards/AnimationCard';
import * as MediaCard from './cards/MediaCard';
import * as CardMedia from './cards/CardMedia';
import * as Keyboard from './cards/Keyboard';
import * as Middleware from './middleware/Middleware';
import * as IntentRecognizerSet from './dialogs/IntentRecognizerSet';
import * as RegExpRecognizer from './dialogs/RegExpRecognizer';
import * as LocalizedRegExpRecognizer from './dialogs/LocalizedRegExpRecognizer';
import * as RecognizerFilter from './dialogs/RecognizerFilter';
import * as LuisRecognizer from './dialogs/LuisRecognizer';
import * as IntentDialog from './dialogs/IntentDialog';

declare var exports: any;

exports.Session = Session.Session;
exports.Message = Message.Message;
exports.AttachmentLayout = Message.AttachmentLayout;
exports.TextFormat = Message.TextFormat;
exports.CardAction = CardAction.CardAction;
exports.HeroCard = HeroCard.HeroCard;
exports.VideoCard = VideoCard.VideoCard;
exports.AudioCard = AudioCard.AudioCard;
exports.AnimationCard = AnimationCard.AnimationCard;
exports.MediaCard = MediaCard.MediaCard;
exports.CardMedia = CardMedia.CardMedia;
exports.CardImage = CardImage.CardImage;
exports.ReceiptCard = ReceiptCard.ReceiptCard;
exports.ReceiptItem = ReceiptCard.ReceiptItem;
exports.Fact = ReceiptCard.Fact;
exports.SigninCard = SigninCard.SigninCard;
exports.ThumbnailCard = ThumbnailCard.ThumbnailCard;
exports.Keyboard = Keyboard.Keyboard;
exports.Dialog = Dialog.Dialog;
exports.ResumeReason = Dialog.ResumeReason;
exports.DialogAction = DialogAction.DialogAction;
exports.PromptType = Prompts.PromptType;
exports.ListStyle = Prompts.ListStyle;
exports.Prompts = Prompts.Prompts;
exports.SimplePromptRecognizer = Prompts.SimplePromptRecognizer;
exports.RecognizeOrder = IntentRecognizerSet.RecognizeOrder;
exports.IntentRecognizerSet = IntentRecognizerSet.IntentRecognizerSet;
exports.IntentDialog = IntentDialog.IntentDialog;
exports.RecognizeMode = IntentDialog.RecognizeMode;
exports.LuisRecognizer = LuisRecognizer.LuisRecognizer;
exports.RegExpRecognizer = RegExpRecognizer.RegExpRecognizer;
exports.LocalizedRegExpRecognizer = LocalizedRegExpRecognizer.LocalizedRegExpRecognizer;
exports.RecognizerFilter = RecognizerFilter.RecognizerFilter;
exports.SimpleDialog = SimpleDialog.SimpleDialog;
exports.EntityRecognizer = EntityRecognizer.EntityRecognizer;
exports.Library = Library.Library;
exports.UniversalBot = UniversalBot.UniversalBot;
exports.ChatConnector = ChatConnector.ChatConnector;
exports.ConsoleConnector = ConsoleConnector.ConsoleConnector;
exports.MemoryBotStorage = BotStorage.MemoryBotStorage;
exports.Middleware = Middleware.Middleware;

// Deprecated classes
import * as deprecatedBCB from './deprecated/BotConnectorBot';
import * as deprecatedLD from './deprecated/LuisDialog';
import * as deprecatedCD from './deprecated/CommandDialog';
import * as deprecatedTB from './deprecated/TextBot';
exports.BotConnectorBot = deprecatedBCB.BotConnectorBot;
exports.LuisDialog = deprecatedLD.LuisDialog;
exports.CommandDialog = deprecatedCD.CommandDialog;
exports.TextBot = deprecatedTB.TextBot;
