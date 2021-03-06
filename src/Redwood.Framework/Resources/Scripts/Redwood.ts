﻿/// <reference path="typings/knockout/knockout.d.ts" />
/// <reference path="typings/knockout.mapper/knockout.mapper.d.ts" />
/// <reference path="typings/globalize/globalize.d.ts" />

class Redwood { 

    private postBackCounter = 0;

    public extensions: any = {}; 
    public viewModels: any = {};
    public culture: string;
    public events = {
        init: new RedwoodEvent<RedwoodEventArgs>("redwood.events.init", true),
        beforePostback: new RedwoodEvent<RedwoodBeforePostBackEventArgs>("redwood.events.beforePostback"),
        afterPostback: new RedwoodEvent<RedwoodAfterPostBackEventArgs>("redwood.events.afterPostback"),
        error: new RedwoodEvent<RedwoodErrorEventArgs>("redwood.events.error")
    };

    public init(viewModelName: string, culture: string): void {
        this.culture = culture;
        this.viewModels[viewModelName].viewModel = ko.mapper.fromJS(this.viewModels[viewModelName].viewModel);

        var viewModel = this.viewModels[viewModelName].viewModel;
        ko.applyBindings(viewModel);
        this.events.init.trigger(new RedwoodEventArgs(viewModel));
    }
    
    public postBack(viewModelName: string, sender: HTMLElement, path: string[], command: string, controlUniqueId: string, validationTargetPath?: any): void {
        var viewModel = this.viewModels[viewModelName].viewModel;

        // prevent double postbacks
        this.postBackCounter++;
        var currentPostBackCounter = this.postBackCounter;

        // trigger beforePostback event
        var beforePostbackArgs = new RedwoodBeforePostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath);
        this.events.beforePostback.trigger(beforePostbackArgs);
        if (beforePostbackArgs.cancel) {
            return;
        }

        // perform the postback
        this.updateDynamicPathFragments(sender, path);
        var data = {
            viewModel: ko.mapper.toJS(viewModel),
            currentPath: path,
            command: command,
            controlUniqueId: controlUniqueId,
            validationTargetPath: validationTargetPath || null
        };
        this.postJSON(document.location.href, "POST", ko.toJSON(data), result => {
            // if another postback has already been passed, don't do anything
            if (this.postBackCounter !== currentPostBackCounter) return;

            var resultObject = JSON.parse(result.responseText);
            if (!resultObject.viewModel && resultObject.viewModelDiff) {
                resultObject.viewModel = this.patch(data.viewModel, resultObject.viewModelDiff);
            }

            var isSuccess = false;
            if (resultObject.action === "successfulCommand") {
                // remove updated controls
                var updatedControls = {};
                for (var id in resultObject.updatedControls) {
                    if (resultObject.updatedControls.hasOwnProperty(id)) {
                        var control = document.getElementById(id);
                        var nextSibling = control.nextSibling;
                        var parent = control.parentNode;
                        ko.removeNode(control);
                        updatedControls[id] = { control: control, nextSibling: nextSibling, parent: parent };
                    }
                }

                // update the viewmodel
                if (resultObject.viewModel)
                    ko.mapper.fromJS(resultObject.viewModel, {}, this.viewModels[viewModelName].viewModel);
                isSuccess = true;

                // add updated controls
                for (id in resultObject.updatedControls) {
                    if (resultObject.updatedControls.hasOwnProperty(id)) {
                        var updatedControl = updatedControls[id];
                        if (updatedControl.nextSibling) {
                            updatedControl.parent.insertBefore(updatedControl.control, updatedControl.nextSibling);
                        } else {
                            updatedControl.parent.appendChild(updatedControl.control);
                        }
                        updatedControl.control.outerHTML = resultObject.updatedControls[id];
                        ko.applyBindings(ko.dataFor(updatedControl.parent), updatedControl.control);
                    }
                }

            } else if (resultObject.action === "redirect") {
                // redirect
                document.location.href = resultObject.url;
                return;
            } 
            
            // trigger afterPostback event
            var afterPostBackArgs = new RedwoodAfterPostBackEventArgs(sender, viewModel, viewModelName, validationTargetPath, resultObject);
            this.events.afterPostback.trigger(afterPostBackArgs);
            if (!isSuccess && !afterPostBackArgs.isHandled) {
                throw "Invalid response from server!";
            }
        }, xhr => {
            // if another postback has already been passed, don't do anything
            if (this.postBackCounter !== currentPostBackCounter) return;

            // execute error handlers
            var errArgs = new RedwoodErrorEventArgs(viewModel, xhr);
            this.events.error.trigger(errArgs);
            if (!errArgs.handled) {
                alert(xhr.responseText);
            }
        });
    }

    public patch(source: any, patch: any): any {
        if (source instanceof Array && patch instanceof Array) {
            return patch.map((val, i) =>
                this.patch(source[i], val));
        }
        else if (source instanceof Array || patch instanceof Array)
            return patch;
        else if (typeof source == "object" && typeof patch == "object") {
            for (var p in patch) {
                if (patch[p] == null) delete source[p];
                else source[p] = this.patch(source[p], patch[p]);
            }
        }
        else return patch;

        return source;
    }

    public formatString(format: string, value: any) {
        if (format == "g") {
            return redwood.formatString("d", value) + " " + redwood.formatString("t", value);
        } else if (format == "G") {
            return redwood.formatString("d", value) + " " + redwood.formatString("T", value);
        }

        value = ko.unwrap(value);
        if (typeof value === "string" && value.match("^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\\.[0-9]{1,3})?$")) {
            // JSON date in string
            value = new Date(value);
        }
        return Globalize.format(value, format, redwood.culture);
    }

    public getDataSourceItems(viewModel: any) {
        var value = ko.unwrap(viewModel);
        return value.Items || value;
    }

    private updateDynamicPathFragments(sender: HTMLElement, path: string[]): void {
        var context = ko.contextFor(sender);

        for (var i = path.length - 1; i >= 0; i--) {
            if (path[i].indexOf("[$index]") >= 0) {
                path[i] = path[i].replace("[$index]", "[" + context.$index() + "]");
            }
            context = context.$parentContext;
        }
    }

    private postJSON(url: string, method: string, postData: any, success: (request: XMLHttpRequest) => void, error: (response: XMLHttpRequest) => void) {
        var xhr = XMLHttpRequest ? new XMLHttpRequest() : <XMLHttpRequest>new ActiveXObject("Microsoft.XMLHTTP");
        xhr.open(method, url, true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.onreadystatechange = () => {
            if (xhr.readyState != 4) return;
            if (xhr.status < 400) {
                success(xhr);
            } else {
                error(xhr);
            }
        };
        xhr.send(postData);
    }

    public evaluateOnViewModel(context, expression) {
        return eval("(function (c) { return c." + expression + "; })")(context);
    }
}

// RedwoodEvent is used because CustomEvent is not browser compatible and does not support 
// calling missed events for handler that subscribed too late.
class RedwoodEvent<T extends RedwoodEventArgs> {
    private handlers = [];
    private history = [];

    constructor(public name: string, private triggerMissedEventsOnSubscribe: boolean = false) {
    }

    public subscribe(handler: (data: T) => void) {
        this.handlers.push(handler);

        if (this.triggerMissedEventsOnSubscribe) {
            for (var i = 0; i < this.history.length; i++) {
                handler(history[i]);
            }
        }
    }

    public unsubscribe(handler: (data: T) => void) {
        var index = this.handlers.indexOf(handler);
        if (index >= 0) {
            this.handlers = this.handlers.splice(index, 1);
        }
    }

    public trigger(data: T): void {
        for (var i = 0; i < this.handlers.length; i++) {
            this.handlers[i](data);
        }

        if (this.triggerMissedEventsOnSubscribe) {
            this.history.push(data);
        }
    }
}

class RedwoodEventArgs {
    constructor(public viewModel: any) {
    }
}
class RedwoodErrorEventArgs extends RedwoodEventArgs {
    public handled = false;
    constructor(public viewModel: any, public xhr: XMLHttpRequest) {
        super(viewModel);
    }
}
class RedwoodBeforePostBackEventArgs extends RedwoodEventArgs {
    public cancel: boolean = false;
    constructor(public sender: HTMLElement, public viewModel: any, public viewModelName: string, public validationTargetPath: any) {
        super(viewModel);
    }
}
class RedwoodAfterPostBackEventArgs extends RedwoodEventArgs {
    public isHandled: boolean = false;
    constructor(public sender: HTMLElement, public viewModel: any, public viewModelName: string, public validationTargetPath: any, public serverResponseObject: any) {
        super(viewModel);
    }
}

var redwood = new Redwood();


// add knockout binding handler for update progress control
ko.bindingHandlers["redwoodUpdateProgressVisible"] = {
    init(element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
        element.style.display = "none";
        redwood.events.beforePostback.subscribe(e => {
            element.style.display = "";
        });
        redwood.events.afterPostback.subscribe(e => {
            element.style.display = "none";
        });
        redwood.events.error.subscribe(e => {
            element.style.display = "none";
        });
    }
};