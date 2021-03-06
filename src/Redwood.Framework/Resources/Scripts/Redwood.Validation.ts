﻿/// <reference path="typings/knockout/knockout.d.ts" />
/// <reference path="typings/knockout.mapper/knockout.mapper.d.ts" />

class RedwoodValidationContext { 
    constructor(public valueToValidate: any, public parentViewModel: any, public parameters: any[]) {
    }
}

class RedwoodValidatorBase {
    public isValid(context: RedwoodValidationContext): boolean {
        return false;
    }
    public isEmpty(value: string): boolean {
        return value == null || /^\s*$/.test(value);
    }
}

class RedwoodRequiredValidator extends RedwoodValidatorBase {
    public isValid(context: RedwoodValidationContext): boolean {
        var value = context.valueToValidate;
        return !this.isEmpty(value);
    }
}
class RedwoodRegularExpressionValidator extends RedwoodValidatorBase {
    public isValid(context: RedwoodValidationContext): boolean {
        var value = context.valueToValidate;
        var expr = context.parameters[0];
        return this.isEmpty(value) || new RegExp(expr).test(value);
    }
}

class ValidationError {
    public errorMessage = ko.observable("");
    public isValid = ko.computed(() => this.errorMessage());
    
    constructor(public targetObservable: KnockoutObservable<any>) {
    }

    public static getOrCreate(targetObservable: KnockoutObservable<any>): ValidationError {
        if (!targetObservable["validationError"]) {
            targetObservable["validationError"] = new ValidationError(targetObservable);
        }
        return <ValidationError>targetObservable["validationError"];
    }
}

interface IRedwoodValidationRules {
    [name: string]: RedwoodValidatorBase;
}
interface IRedwoodValidationElementUpdateFunctions {
    [name: string]: (element: any, errorMessage: string, options: any) => void;
}

class RedwoodValidation
{
    public rules: IRedwoodValidationRules = {
        "required": new RedwoodRequiredValidator(),
        "regularExpression": new RedwoodRegularExpressionValidator()
        //"numeric": new RedwoodNumericValidator(),
        //"datetime": new RedwoodDateTimeValidator(),
        //"range": new RedwoodRangeValidator()
    }

    public errors = ko.observableArray<ValidationError>([]);

    public elementUpdateFunctions: IRedwoodValidationElementUpdateFunctions = {
        
        // shows the element when it is not valid
        hideWhenValid(element: any, errorMessage: string, options: any) {
            if (errorMessage) {
                element.style.display = "";
                element.title = "";
            } else {
                element.style.display = "none";
                element.title = errorMessage;
            }
        },

        // adds a CSS class when the element is not valid
        addCssClass(element: any, errorMessage: string, options: any) {
            var cssClass = (options && options.cssClass) ? options.cssClass : "field-validation-error";
            if (errorMessage) {
                element.className += " " + cssClass;
            } else {
                element.className = element.className.replace(new RegExp("\\b" + cssClass + "\\b", "g"), "");
            }
        },

        // displays the error message
        displayErrorMessage(element: any, errorMessage: string, options: any) {
            element[element.innerText ? "innerText" : "textContent"] = errorMessage;
        }
    }

    /// Validates the specified view model
    public validateViewModel(viewModel: any) {
        if (!viewModel || !viewModel.$type || !redwood.viewModels.root.validationRules) return;

        // find validation rules
        var type = ko.unwrap(viewModel.$type);
        if (!type) return;
        var rulesForType = redwood.viewModels.root.validationRules[type];
        if (!rulesForType) return;

        // validate all properties
        for (var property in viewModel) {
            if (!viewModel.hasOwnProperty(property) || property.indexOf("$") >= 0) continue;

            var viewModelProperty = viewModel[property];
            if (!viewModelProperty || !ko.isObservable(viewModelProperty)) continue;

            var value = viewModel[property]();

            // run validation rules
            if (rulesForType.hasOwnProperty(property)) {
                this.validateProperty(viewModel, viewModelProperty, value, rulesForType[property]);
            }

            if (value) {
                if (Array.isArray(value))
                {
                    // handle collections
                    for (var i = 0; i < value.length; i++) {
                        this.validateViewModel(value[i]);
                    }
                }
                else if (value.$type) {
                    // handle nested objects
                    this.validateViewModel(value);
                }
            }
        }
    }

    /// Validates the specified property in the viewModel
    public validateProperty(viewModel: any, property: KnockoutObservable<any>, value: any, rulesForProperty: any[]) {
        for (var i = 0; i < rulesForProperty.length; i++) {
            // validate the rules
            var rule = rulesForProperty[i];
            var ruleTemplate = this.rules[rule.ruleName];
            var context = new RedwoodValidationContext(value, viewModel, rule.parameters);

            var validationError = ValidationError.getOrCreate(property);
            if (!ruleTemplate.isValid(context)) {
                // add error message
                validationError.errorMessage(rule.errorMessage);
                this.addValidationError(viewModel, validationError);
            } else {
                // remove
                validationError.errorMessage("");
                this.removeValidationError(viewModel, validationError);
            }
        }
    }

    // clears validation errors
    public clearValidationErrors() {
        var errors = [];
        for (var i = 0; i < errors.length; i++) {
            errors[i].errorMessage("");
        }
        this.errors.removeAll();
    }

    // merge validation rules
    public mergeValidationRules(args: RedwoodAfterPostBackEventArgs) {
        if (args.serverResponseObject.validationRules) {
            var existingRules = redwood.viewModels[args.viewModelName].validationRules;

            for (var type in args.serverResponseObject) {
                if (!args.serverResponseObject.hasOwnProperty(type)) continue;
                existingRules[type] = args.serverResponseObject[type];
            }
        }
    }

    // shows the validation errors from server
    public showValidationErrorsFromServer(args: RedwoodAfterPostBackEventArgs) {
        // resolve validation target
        var context = ko.contextFor(args.sender);
        var validationTarget = redwood.evaluateOnViewModel(context, args.validationTargetPath);

        // add validation errors
        this.clearValidationErrors();
        var modelState = args.serverResponseObject.modelState;
        for (var i = 0; i < modelState.length; i++) {
            // find the observable property
            var propertyPath = modelState[i].propertyPath;
            var observable = redwood.evaluateOnViewModel(validationTarget, propertyPath);
            var parent = redwood.evaluateOnViewModel(context, propertyPath.substring(0, propertyPath.lastIndexOf(".")) || "$data");
            if (!ko.isObservable(observable) || !parent || !parent.$validationErrors) {
                throw "Invalid validation path!";
            }

            // add the error to appropriate collections
            var error = ValidationError.getOrCreate(observable);
            error.errorMessage(modelState[i].errorMessage);
            this.addValidationError(parent, error);
        }
    }

    private addValidationError(viewModel: any, error: ValidationError) {
        if (viewModel.$validationErrors.indexOf(error) < 0) {
            viewModel.$validationErrors.push(error);
        }
        this.errors.push(error);
    }

    private removeValidationError(viewModel: any, error: ValidationError) {
        viewModel.$validationErrors.push(error);
        this.errors.remove(error);
    }
};

// init the plugin
declare var redwood: Redwood;
if (!redwood) {
    throw "Redwood.js is required!";
}
redwood.extensions.validation = redwood.extensions.validation || new RedwoodValidation();

// perform the validation before postback
redwood.events.beforePostback.subscribe(args => {
    if (args.validationTargetPath) {
        // resolve target
        var context = ko.contextFor(args.sender);
        var validationTarget = redwood.evaluateOnViewModel(context, args.validationTargetPath);
        
        // validate the object
        redwood.extensions.validation.clearValidationErrors();
        redwood.extensions.validation.validateViewModel(validationTarget);
        if (redwood.extensions.validation.errors().length > 0) {
            args.cancel = true;
        }
    }
});

redwood.events.afterPostback.subscribe(args => {
    if (args.serverResponseObject.action === "successfulCommand") {
        // merge validation rules from postback with those we already have (required when a new type appears in the view model)
        redwood.extensions.validation.mergeValidationRules(args);
        args.isHandled = true;
    } else if (args.serverResponseObject.action === "validationErrors") {
        // apply validation errors from server
        redwood.extensions.validation.showValidationErrorsFromServer(args);
        args.isHandled = true;
    }
});

// add knockout binding handler
ko.bindingHandlers["redwoodValidation"] = {
    init (element: any, valueAccessor: () => any, allBindingsAccessor: KnockoutAllBindingsAccessor, viewModel: any, bindingContext: KnockoutBindingContext) {
        var observableProperty = valueAccessor();
        if (ko.isObservable(observableProperty)) {
            // try to get the options
            var options = allBindingsAccessor.get("redwoodValidationOptions");
            var mode = (options && options.mode) ? options.mode : "addCssClass";
            var updateFunction = redwood.extensions.validation.elementUpdateFunctions[mode];

            // subscribe to the observable property changes
            var validationError = ValidationError.getOrCreate(observableProperty);
            validationError.errorMessage.subscribe(newValue => updateFunction(element, newValue, options));
            updateFunction(element, validationError.errorMessage(), options);
        }
    }
};


