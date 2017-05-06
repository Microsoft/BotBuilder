var assert = require('assert');
var builder = require('../');

describe('recognizers', function() {
    this.timeout(5000);
    it('should match a RegExpRecognizer', function (done) { 
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Not Matched').endDialog();
        });
        bot.recognizer(new builder.RegExpRecognizer('HelpIntent', /^help/i));
        bot.dialog('testDialog', function (session) {
            session.send('Matched').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            assert(message.text === 'Matched');
            done();
        });
        connector.processMessage('help');
    });

    it('should NOT match a RegExpRecognizer', function (done) { 
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Not Matched').endDialog();
        });
        bot.recognizer(new builder.RegExpRecognizer('HelpIntent', /^help/i));
        bot.dialog('testDialog', function (session) {
            session.send('Matched').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            assert(message.text === 'Not Matched');
            done();
        });
        connector.processMessage('hello');
    });

    it('should match a LocalizedRegExpRecognizer', function (done) { 
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Not Matched').endDialog();
        });
        bot.recognizer(new builder.LocalizedRegExpRecognizer('HelpIntent', "exp1"));
        bot.dialog('testDialog', function (session) {
            session.send('Matched').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            assert(message.text === 'Matched');
            done();
        });
        connector.processMessage('help');
    });

    it('should NOT match a LocalizedRegExpRecognizer', function (done) { 
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Not Matched').endDialog();
        });
        bot.recognizer(new builder.LocalizedRegExpRecognizer('HelpIntent', "exp1"));
        bot.dialog('testDialog', function (session) {
            session.send('Matched').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            assert(message.text === 'Not Matched');
            done();
        });
        connector.processMessage('hello');
    });

    it('should enable/disable a RecognizerFilter', function (done) {
        var step = 0;
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Filtered').endDialog();
        });
        var recognizer = new builder.RecognizerFilter(new builder.RegExpRecognizer('HelpIntent', /^help/i))
            .onEnabled(function (context, callback) {
                callback(null, step === 0);
            });
        bot.recognizer(recognizer);
        bot.dialog('testDialog', function (session) {
            session.send('Not Filtered').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            switch (step++) {
                case 0:
                    assert(message.text === 'Not Filtered');
                    connector.processMessage('help');
                    break;
                default:
                    assert(message.text === 'Filtered');
                    done();                    
            }
        });
        connector.processMessage('help');
    });

    it('should filter the output using a RecognizerFilter', function (done) {
        var step = 0;
        var connector = new builder.ConsoleConnector();       
        var bot = new builder.UniversalBot(connector, function (session, args) {
            session.send('Filtered').endDialog();
        });
        var recognizer = new builder.RecognizerFilter(new builder.RegExpRecognizer('HelpIntent', /^help/i))
            .onRecognized(function (context, result, callback) {
                callback(null, step === 0 ? result : { score: 0.0, intent: null });
            });
        bot.recognizer(recognizer);
        bot.dialog('testDialog', function (session) {
            session.send('Not Filtered').endDialog();
        }).triggerAction({ matches: 'HelpIntent' });
        bot.on('send', function (message) {
            switch (step++) {
                case 0:
                    assert(message.text === 'Not Filtered');
                    connector.processMessage('help');
                    break;
                default:
                    assert(message.text === 'Filtered');
                    done();                    
            }
        });
        connector.processMessage('help');
    });
});