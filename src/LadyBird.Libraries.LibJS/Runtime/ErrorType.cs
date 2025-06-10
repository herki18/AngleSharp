// File: ErrorTypes.cs
/*
 * Copyright (c) 2020, Matthew Olsson <mattco@serenityos.org>
 *
 * SPDX-License-Identifier: BSD-2-Clause
 */

namespace JS.Runtime;

// This file implements the ErrorType class as in the original C++ code.
// All comments and logic are preserved as much as possible.

public sealed class ErrorType
{
    // C++: static const ErrorType name;
    // In C#, we use static readonly fields.
    // The following fields are generated from the JS_ENUMERATE_ERROR_TYPES macro.

    // BEGIN GENERATED ERROR TYPES
    public static readonly ErrorType AccessorBadField = new("Accessor descriptor's '{}' field must be a function or undefined");
    public static readonly ErrorType AccessorValueOrWritable = new("Accessor property descriptor cannot specify a value or writable key");
    public static readonly ErrorType AgentCannotSuspend = new("Agent is not allowed to suspend");
    public static readonly ErrorType ArrayMaxSize = new("Maximum array size exceeded");
    public static readonly ErrorType AsyncDisposableStackAlreadyDisposed = new("AsyncDisposableStack is already disposed");
    public static readonly ErrorType BadArgCountMany = new("{}() needs {} arguments");
    public static readonly ErrorType BadArgCountOne = new("{}() needs one argument");
    public static readonly ErrorType BigIntBadOperator = new("Cannot use {} operator with BigInt");
    public static readonly ErrorType BigIntBadOperatorOtherType = new("Cannot use {} operator with BigInt and other type");
    public static readonly ErrorType BigIntFromNonIntegral = new("Cannot convert non-integral number to BigInt");
    public static readonly ErrorType BigIntInvalidValue = new("Invalid value for BigInt: {}");
    public static readonly ErrorType BigIntSizeExceeded = new("Maximum BigInt size exceeded");
    public static readonly ErrorType BindingNotInitialized = new("Binding {} is not initialized");
    public static readonly ErrorType BufferOutOfBounds = new("{} contains a property which references a value at an index not contained within its buffer's bounds");
    public static readonly ErrorType ByteLengthExceedsMaxByteLength = new("ArrayBuffer byte length of {} exceeds the max byte length of {}");
    public static readonly ErrorType CallStackSizeExceeded = new("Call stack size limit exceeded");
    public static readonly ErrorType CannotBeHeldWeakly = new("{} cannot be held weakly");
    public static readonly ErrorType CannotDeclareGlobalFunction = new("Cannot declare global function of name '{}'");
    public static readonly ErrorType CannotDeclareGlobalVariable = new("Cannot declare global variable of name '{}'");
    public static readonly ErrorType ClassConstructorWithoutNew = new("Class constructor {} must be called with 'new'");
    public static readonly ErrorType ClassExtendsValueInvalidPrototype = new("Class extends value has an invalid prototype {}");
    public static readonly ErrorType ClassExtendsValueNotAConstructorOrNull = new("Class extends value {} is not a constructor or null");
    public static readonly ErrorType ClassIsAbstract = new("Abstract class {} cannot be constructed directly");
    public static readonly ErrorType ConstructorWithoutNew = new("{} constructor must be called with 'new'");
    public static readonly ErrorType Convert = new("Cannot convert {} to {}");
    public static readonly ErrorType DataViewOutOfRangeByteOffset = new("Data view byte offset {} is out of range for buffer with length {}");
    public static readonly ErrorType DerivedConstructorReturningInvalidValue = new("Derived constructor return invalid value");
    public static readonly ErrorType DescWriteNonWritable = new("Cannot write to non-writable property '{}'");
    public static readonly ErrorType DetachedArrayBuffer = new("ArrayBuffer is detached");
    public static readonly ErrorType DetachKeyMismatch = new("Provided detach key {} does not match the ArrayBuffer's detach key {}");
    public static readonly ErrorType DisposableStackAlreadyDisposed = new("DisposableStack already disposed values");
    public static readonly ErrorType DivisionByZero = new("Division by zero");
    public static readonly ErrorType DynamicImportNotAllowed = new("Dynamic Imports are not allowed");
    public static readonly ErrorType FinalizationRegistrySameTargetAndValue = new("Target and held value must not be the same");
    public static readonly ErrorType FixedArrayBuffer = new("ArrayBuffer is not resizable");
    public static readonly ErrorType GeneratorAlreadyExecuting = new("Generator is already executing");
    public static readonly ErrorType GeneratorBrandMismatch = new("Generator brand '{}' does not match generator brand '{}')");
    public static readonly ErrorType GetCapabilitiesExecutorCalledMultipleTimes = new("GetCapabilitiesExecutor was called multiple times");
    public static readonly ErrorType GetLegacyRegExpStaticPropertyThisValueMismatch = new("Legacy RegExp static property getter must be called with the RegExp constructor for the this value");
    public static readonly ErrorType GetLegacyRegExpStaticPropertyValueEmpty = new("Legacy RegExp static property getter value is empty");
    public static readonly ErrorType GlobalEnvironmentAlreadyHasBinding = new("Global environment already has binding '{}'");
    public static readonly ErrorType ImportAttributeUnsupported = new("Every import attribute is not supported");
    public static readonly ErrorType IndexOutOfRange = new("Index {} is out of range of array length {}");
    public static readonly ErrorType InOperatorWithObject = new("'in' operator must be used on an object");
    public static readonly ErrorType InstanceOfOperatorBadPrototype = new("'prototype' property of {} is not an object");
    public static readonly ErrorType IntlFractionalUnitFollowedByNonFractionalUnit = new("Non-fractional unit {} is not allowed after a fractional unit");
    public static readonly ErrorType IntlFractionalUnitsMixedWithAlwaysDisplay = new("Fractional unit {} may not be used with {} value of 'always'");
    public static readonly ErrorType IntlInvalidDateTimeFormatOption = new("Option {} cannot be set when also providing {}");
    public static readonly ErrorType IntlInvalidKey = new("{} is not a valid key");
    public static readonly ErrorType IntlInvalidLanguageTag = new("{} is not a structurally valid language tag");
    public static readonly ErrorType IntlInvalidRoundingIncrement = new("{} is not a valid rounding increment");
    public static readonly ErrorType IntlInvalidRoundingIncrementForFractionDigits = new("{} is not a valid rounding increment for inequal min/max fraction digits");
    public static readonly ErrorType IntlInvalidRoundingIncrementForRoundingType = new("{} is not a valid rounding increment for rounding type {}");
    public static readonly ErrorType IntlInvalidTime = new("Time value must be between -8.64E15 and 8.64E15");
    public static readonly ErrorType IntlInvalidUnit = new("Unit {} is not a valid time unit");
    public static readonly ErrorType IntlMinimumExceedsMaximum = new("Minimum value {} is larger than maximum value {}");
    public static readonly ErrorType IntlNonNumericOr2DigitAfterNumericOr2Digit = new("Styles other than 'fractional', numeric', or '2-digit' may not be used in smaller units after being used in larger units");
    public static readonly ErrorType IntlNumberIsNaNOrOutOfRange = new("Value {} is NaN or is not between {} and {}");
    public static readonly ErrorType IntlOptionUndefined = new("Option {} must be defined when option {} is {}");
    public static readonly ErrorType IntlTemporalFormatIsNull = new("Unable to determine format for {}");
    public static readonly ErrorType IntlTemporalFormatRangeTypeMismatch = new("Cannot format a date-time range with different date-time types");
    public static readonly ErrorType IntlTemporalInvalidCalendar = new("Cannot format {} with calendar '{}' in locale with calendar '{}'");
    public static readonly ErrorType IntlTemporalZonedDateTime = new("Cannot format Temporal.ZonedDateTime, use Temporal.ZonedDateTime.prototype.toLocaleString");
    public static readonly ErrorType InvalidAssignToConst = new("Invalid assignment to const variable");
    public static readonly ErrorType InvalidCodePoint = new("Invalid code point {}, must be an integer no less than 0 and no greater than 0x10FFFF");
    public static readonly ErrorType InvalidEnumerationValue = new("Invalid value '{}' for enumeration type '{}'");
    public static readonly ErrorType InvalidFractionDigits = new("Fraction Digits must be an integer no less than 0, and no greater than 100");
    public static readonly ErrorType InvalidHint = new("Invalid hint: \"{}\"");
    public static readonly ErrorType InvalidIndex = new("Index must be a positive integer no greater than 2^53-1");
    public static readonly ErrorType InvalidLeftHandAssignment = new("Invalid left-hand side in assignment");
    public static readonly ErrorType InvalidLength = new("Invalid {} length");
    public static readonly ErrorType InvalidNormalizationForm = new("The normalization form must be one of NFC, NFD, NFKC, NFKD. Got '{}'");
    public static readonly ErrorType InvalidOrAmbiguousExportEntry = new("Invalid or ambiguous export entry '{}'");
    public static readonly ErrorType InvalidPrecision = new("Precision must be an integer no less than 1, and no greater than 100");
    public static readonly ErrorType InvalidRadix = new("Radix must be an integer no less than 2, and no greater than 36");
    public static readonly ErrorType InvalidRestrictedFloatingPointParameter = new("Expected {} to be a finite floating-point number");
    public static readonly ErrorType InvalidTimeValue = new("Invalid time value");
    public static readonly ErrorType IsNotA = new("{} is not a {}");
    public static readonly ErrorType IsNotAEvaluatedFrom = new("{} is not a {} (evaluated from '{}')");
    public static readonly ErrorType IsNotAn = new("{} is not an {}");
    public static readonly ErrorType IsUndefined = new("{} is undefined");
    public static readonly ErrorType IterableNextBadReturn = new("iterator.next() returned a non-object value");
    public static readonly ErrorType IterableReturnBadReturn = new("iterator.return() returned a non-object value");
    public static readonly ErrorType JsonBigInt = new("Cannot serialize BigInt value to JSON");
    public static readonly ErrorType JsonCircular = new("Cannot stringify circular object");
    public static readonly ErrorType JsonMalformed = new("Malformed JSON string");
    public static readonly ErrorType JsonRawJSONNonPrimitive = new("JSON.rawJSON cannot accept object or array as outermost value");
    public static readonly ErrorType MathSumPreciseOverflow = new("Overflow in Math.sumPrecise");
    public static readonly ErrorType MissingRequiredProperty = new("Required property {} is missing or undefined");
    public static readonly ErrorType ModuleNoEnvironment = new("Cannot find module environment for imported binding");
    public static readonly ErrorType ModuleNotFound = new("Cannot find/open module: '{}'");
    public static readonly ErrorType NegativeExponent = new("Exponent must be positive");
    public static readonly ErrorType NoDisposeMethod = new("{} does not have dispose method");
    public static readonly ErrorType NotAConstructor = new("{} is not a constructor");
    public static readonly ErrorType NotAFunction = new("{} is not a function");
    public static readonly ErrorType NotAnIntegerOrUndefined = new("{} is neither an integer nor undefined");
    public static readonly ErrorType NotAnObject = new("{} is not an object");
    public static readonly ErrorType NotAnObjectOfType = new("Not an object of type {}");
    public static readonly ErrorType NotAnObjectOrNull = new("{} is neither an object nor null");
    public static readonly ErrorType NotAnObjectOrString = new("{} is neither an object nor a string");
    public static readonly ErrorType NotASharedArrayBuffer = new("The array buffer object must be a SharedArrayBuffer");
    public static readonly ErrorType NotAString = new("{} is not a string");
    public static readonly ErrorType NotASymbol = new("{} is not a symbol");
    public static readonly ErrorType NotEnoughMemoryToAllocate = new("Not enough memory to allocate {} bytes");
    public static readonly ErrorType NotImplemented = new("TODO({} is not implemented in LibJS)");
    public static readonly ErrorType NotIterable = new("{} is not iterable");
    public static readonly ErrorType NotObjectCoercible = new("{} cannot be converted to an object");
    public static readonly ErrorType NotUndefined = new("{} is not undefined");
    public static readonly ErrorType NumberIsNaN = new("{} must not be NaN");
    public static readonly ErrorType NumberIsNaNOrInfinity = new("Number must not be NaN or Infinity");
    public static readonly ErrorType NumberIsNegative = new("{} must not be negative");
    public static readonly ErrorType ObjectDefineOwnPropertyReturnedFalse = new("Object's [[DefineOwnProperty]] method returned false");
    public static readonly ErrorType ObjectDeleteReturnedFalse = new("Object's [[Delete]] method returned false");
    public static readonly ErrorType ObjectFreezeFailed = new("Could not freeze object");
    public static readonly ErrorType ObjectPreventExtensionsReturnedFalse = new("Object's [[PreventExtensions]] method returned false");
    public static readonly ErrorType ObjectPrototypeWrongType = new("Prototype must be an object or null");
    public static readonly ErrorType ObjectSealFailed = new("Could not seal object");
    public static readonly ErrorType ObjectSetPrototypeOfReturnedFalse = new("Object's [[SetPrototypeOf]] method returned false");
    public static readonly ErrorType ObjectSetReturnedFalse = new("Object's [[Set]] method returned false");
    public static readonly ErrorType OptionIsNotValidValue = new("{} is not a valid value for option {}");
    public static readonly ErrorType OutOfMemory = new("Out of memory");
    public static readonly ErrorType OverloadResolutionFailed = new("Overload resolution failed");
    public static readonly ErrorType PrivateFieldAlreadyDeclared = new("Private field '{}' has already been declared");
    public static readonly ErrorType PrivateFieldDoesNotExistOnObject = new("Private field '{}' does not exist on object");
    public static readonly ErrorType PrivateFieldGetAccessorWithoutGetter = new("Cannot get private field '{}' as accessor without getter");
    public static readonly ErrorType PrivateFieldSetAccessorWithoutSetter = new("Cannot set private field '{}' as accessor without setter");
    public static readonly ErrorType PrivateFieldSetMethod = new("Cannot set private method '{}'");
    public static readonly ErrorType PromiseExecutorNotAFunction = new("Promise executor must be a function");
    public static readonly ErrorType ProxyConstructBadReturnType = new("Proxy handler's construct trap violates invariant: must return an object");
    public static readonly ErrorType ProxyConstructorBadType = new("Expected {} argument of Proxy constructor to be object, got {}");
    public static readonly ErrorType ProxyDefinePropExistingConfigurable = new("Proxy handler's defineProperty trap violates invariant: a property cannot be defined as non-configurable if it already exists on the target object as a configurable property");
    public static readonly ErrorType ProxyDefinePropIncompatibleDescriptor = new("Proxy handler's defineProperty trap violates invariant: the new descriptor is not compatible with the existing descriptor of the property on the target");
    public static readonly ErrorType ProxyDefinePropNonConfigurableNonExisting = new("Proxy handler's defineProperty trap violates invariant: a property cannot be defined as non-configurable if it does not already exist on the target object");
    public static readonly ErrorType ProxyDefinePropNonExtensible = new("Proxy handler's defineProperty trap violates invariant: a property cannot be reported as being defined if the property does not exist on the target and the target is non-extensible");
    public static readonly ErrorType ProxyDefinePropNonWritable = new("Proxy handler's defineProperty trap violates invariant: a non-configurable property cannot be non-writable, unless there exists a corresponding non-configurable, non-writable own property of the target object");
    public static readonly ErrorType ProxyDeleteNonConfigurable = new("Proxy handler's deleteProperty trap violates invariant: cannot report a non-configurable own property of the target as deleted");
    public static readonly ErrorType ProxyDeleteNonExtensible = new("Proxy handler's deleteProperty trap violates invariant: a property cannot be reported as deleted, if it exists as an own property of the target object and the target object is non-extensible. ");
    public static readonly ErrorType ProxyGetImmutableDataProperty = new("Proxy handler's get trap violates invariant: the returned value must match the value on the target if the property exists on the target as a non-writable, non-configurable own data property");
    public static readonly ErrorType ProxyGetNonConfigurableAccessor = new("Proxy handler's get trap violates invariant: the returned value must be undefined if the property exists on the target as a non-configurable accessor property with an undefined get attribute");
    public static readonly ErrorType ProxyGetOwnDescriptorInvalidDescriptor = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: invalid property descriptor for existing property on the target");
    public static readonly ErrorType ProxyGetOwnDescriptorInvalidNonConfig = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: cannot report target's property as non-configurable if the property does not exist, or if it is configurable");
    public static readonly ErrorType ProxyGetOwnDescriptorNonConfigurable = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: cannot return undefined for a property on the target which is a non-configurable property");
    public static readonly ErrorType ProxyGetOwnDescriptorNonConfigurableNonWritable = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: cannot a property as both non-configurable and non-writable, unless it exists as a non-configurable, non-writable own property of the target object");
    public static readonly ErrorType ProxyGetOwnDescriptorReturn = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: must return an object or undefined");
    public static readonly ErrorType ProxyGetOwnDescriptorUndefinedReturn = new("Proxy handler's getOwnPropertyDescriptor trap violates invariant: cannot report a property as being undefined if it exists as an own property of the target and the target is non-extensible");
    public static readonly ErrorType ProxyGetPrototypeOfNonExtensible = new("Proxy handler's getPrototypeOf trap violates invariant: cannot return a different prototype object for a non-extensible target");
    public static readonly ErrorType ProxyGetPrototypeOfReturn = new("Proxy handler's getPrototypeOf trap violates invariant: must return an object or null");
    public static readonly ErrorType ProxyHasExistingNonConfigurable = new("Proxy handler's has trap violates invariant: a property cannot be reported as non-existent if it exists on the target as a non-configurable property");
    public static readonly ErrorType ProxyHasExistingNonExtensible = new("Proxy handler's has trap violates invariant: a property cannot be reported as non-existent if it exists on the target and the target is non-extensible");
    public static readonly ErrorType ProxyIsExtensibleReturn = new("Proxy handler's isExtensible trap violates invariant: return value must match the target's extensibility");
    public static readonly ErrorType ProxyOwnPropertyKeysDuplicates = new("Proxy handler's ownKeys trap violates invariant: the result list may not contain duplicate elements");
    public static readonly ErrorType ProxyOwnPropertyKeysNonExtensibleNewProperty = new("Proxy handler's ownKeys trap violates invariant: cannot report new property '{}' of non-extensible object");
    public static readonly ErrorType ProxyOwnPropertyKeysNonExtensibleSkippedProperty = new("Proxy handler's ownKeys trap violates invariant: cannot skip property '{}' of non-extensible object");
    public static readonly ErrorType ProxyOwnPropertyKeysNotStringOrSymbol = new("Proxy handler's ownKeys trap violates invariant: the type of each result list element is either String or Symbol");
    public static readonly ErrorType ProxyOwnPropertyKeysSkippedNonconfigurableProperty = new("Proxy handler's ownKeys trap violates invariant: cannot skip non-configurable property '{}'");
    public static readonly ErrorType ProxyPreventExtensionsReturn = new("Proxy handler's preventExtensions trap violates invariant: cannot return true if the target object is extensible");
    public static readonly ErrorType ProxyRevoked = new("An operation was performed on a revoked Proxy object");
    public static readonly ErrorType ProxySetImmutableDataProperty = new("Proxy handler's set trap violates invariant: cannot return true for a property on the target which is a non-configurable, non-writable own data property");
    public static readonly ErrorType ProxySetNonConfigurableAccessor = new("Proxy handler's set trap violates invariant: cannot return true for a property on the target which is a non-configurable own accessor property with an undefined set attribute");
    public static readonly ErrorType ProxySetPrototypeOfNonExtensible = new("Proxy handler's setPrototypeOf trap violates invariant: the argument must match the prototype of the target if the target is non-extensible");
    public static readonly ErrorType ReduceNoInitial = new("Reduce of empty array with no initial value");
    public static readonly ErrorType ReferenceNullishDeleteProperty = new("Cannot delete property '{}' of {}");
    public static readonly ErrorType ReferenceNullishSetProperty = new("Cannot set property '{}' of {}");
    public static readonly ErrorType ReferencePrimitiveSetProperty = new("Cannot set property '{}' of {} '{}'");
    public static readonly ErrorType ReferenceUnresolvable = new("Unresolvable reference");
    public static readonly ErrorType RegExpCompileError = new("RegExp compile error: {}");
    public static readonly ErrorType RegExpObjectBadFlag = new("Invalid RegExp flag '{}'");
    public static readonly ErrorType RegExpObjectIncompatibleFlags = new("RegExp flag '{}' is incompatible with flag '{}'");
    public static readonly ErrorType RegExpObjectRepeatedFlag = new("Repeated RegExp flag '{}'");
    public static readonly ErrorType RestrictedFunctionPropertiesAccess = new("Restricted function properties like 'callee', 'caller' and 'arguments' may not be accessed in strict mode");
    public static readonly ErrorType RestrictedGlobalProperty = new("Cannot declare global property '{}'");
    public static readonly ErrorType SetLegacyRegExpStaticPropertyThisValueMismatch = new("Legacy RegExp static property setter must be called with the RegExp constructor for the this value");
    public static readonly ErrorType ShadowRealmEvaluateAbruptCompletion = new("The evaluated script did not complete normally");
    public static readonly ErrorType ShadowRealmWrappedValueNonFunctionObject = new("Wrapped value must be primitive or a function object, got {}");
    public static readonly ErrorType SharedArrayBuffer = new("The array buffer object cannot be a SharedArrayBuffer");
    public static readonly ErrorType SpeciesConstructorDidNotCreate = new("Species constructor did not create {}");
    public static readonly ErrorType SpeciesConstructorReturned = new("Species constructor returned {}");
    public static readonly ErrorType StringNonGlobalRegExp = new("RegExp argument is non-global");
    public static readonly ErrorType StringRepeatCountMustBe = new("repeat count must be a {} number");
    public static readonly ErrorType StringRepeatCountMustNotOverflow = new("repeat count must not overflow");
    public static readonly ErrorType TemporalDifferentCalendars = new("Cannot compare dates from two different calendars");
    public static readonly ErrorType TemporalDifferentTimeZones = new("Cannot compare dates from two different time zones");
    public static readonly ErrorType TemporalDisambiguatePossibleEpochNSRejectMoreThanOne = new("Cannot disambiguate two or more possible epoch nanoseconds");
    public static readonly ErrorType TemporalDisambiguatePossibleEpochNSRejectZero = new("Cannot disambiguate zero possible epoch nanoseconds");
    public static readonly ErrorType TemporalInvalidCalendar = new("Invalid calendar");
    public static readonly ErrorType TemporalInvalidCalendarFieldName = new("Invalid calendar field '{}'");
    public static readonly ErrorType TemporalInvalidCalendarIdentifier = new("Invalid calendar identifier '{}'");
    public static readonly ErrorType TemporalInvalidCalendarString = new("Invalid calendar string '{}'");
    public static readonly ErrorType TemporalInvalidCriticalAnnotation = new("Invalid critical annotation: '{}={}'");
    public static readonly ErrorType TemporalInvalidDuration = new("Invalid duration");
    public static readonly ErrorType TemporalInvalidDurationLikeObject = new("Invalid duration-like object");
    public static readonly ErrorType TemporalInvalidDurationPropertyValueNonIntegral = new("Invalid value for duration property '{}': must be an integer, got {}");
    public static readonly ErrorType TemporalInvalidDurationString = new("Invalid duration string '{}'");
    public static readonly ErrorType TemporalInvalidEpochNanoseconds = new("Invalid epoch nanoseconds value, must be in range -86400 * 10^17 to 86400 * 10^17");
    public static readonly ErrorType TemporalInvalidInstantString = new("Invalid instant string '{}'");
    public static readonly ErrorType TemporalInvalidISODate = new("Invalid ISO date");
    public static readonly ErrorType TemporalInvalidISODateTime = new("Invalid ISO date time");
    public static readonly ErrorType TemporalInvalidLargestUnit = new("Largest unit must not be {}");
    public static readonly ErrorType TemporalInvalidMonthCode = new("Invalid month code");
    public static readonly ErrorType TemporalInvalidPlainDate = new("Invalid plain date");
    public static readonly ErrorType TemporalInvalidPlainDateTime = new("Invalid plain date time");
    public static readonly ErrorType TemporalInvalidPlainMonthDay = new("Invalid plain month day");
    public static readonly ErrorType TemporalInvalidPlainTime = new("Invalid plain time");
    public static readonly ErrorType TemporalInvalidPlainYearMonth = new("Invalid plain year month");
    public static readonly ErrorType TemporalInvalidTime = new("Invalid time");
    public static readonly ErrorType TemporalInvalidTimeLikeField = new("Invalid value {} for time field '{}'");
    public static readonly ErrorType TemporalInvalidTimeZoneName = new("Invalid time zone name '{}'");
    public static readonly ErrorType TemporalInvalidTimeZoneString = new("Invalid time zone string '{}'");
    public static readonly ErrorType TemporalInvalidUnitRange = new("Invalid unit range, {} is larger than {}");
    public static readonly ErrorType TemporalInvalidZonedDateTimeOffset = new("Invalid offset for the provided date and time in the current time zone");
    public static readonly ErrorType TemporalInvalidZonedDateTimeString = new("Invalid zoned date time string '{}'");
    public static readonly ErrorType TemporalMissingOptionsObject = new("Required options object is missing or undefined");
    public static readonly ErrorType TemporalMissingStartingPoint = new("A starting point is required for comparing {}");
    public static readonly ErrorType TemporalMissingUnits = new("One or both of smallestUnit or largestUnit is required");
    public static readonly ErrorType TemporalObjectMustBePartialTemporalObject = new("Object must be a partial Temporal object");
    public static readonly ErrorType ThisHasNotBeenInitialized = new("|this| has not been initialized");
    public static readonly ErrorType ThisIsAlreadyInitialized = new("|this| is already initialized");
    public static readonly ErrorType ToObjectNullOrUndefined = new("ToObject on null or undefined");
    public static readonly ErrorType ToObjectNullOrUndefinedWithName = new("\"{}\" is {}");
    public static readonly ErrorType ToObjectNullOrUndefinedWithProperty = new("Cannot access property \"{}\" on {} object");
    public static readonly ErrorType ToObjectNullOrUndefinedWithPropertyAndName = new("Cannot access property \"{}\" on {} object \"{}\"");
    public static readonly ErrorType TopLevelVariableAlreadyDeclared = new("Redeclaration of top level variable '{}'");
    public static readonly ErrorType ToPrimitiveReturnedObject = new("Can't convert {} to primitive with hint \"{}\", its @@toPrimitive method returned an object");
    public static readonly ErrorType TypedArrayContentTypeMismatch = new("Can't create {} from {}");
    public static readonly ErrorType TypedArrayInvalidBufferLength = new("Invalid buffer length for {}: must be a multiple of {}, got {}");
    public static readonly ErrorType TypedArrayInvalidByteOffset = new("Invalid byte offset for {}: must be a multiple of {}, got {}");
    public static readonly ErrorType TypedArrayInvalidCopy = new("Copy between arrays of different content types ({} and {}) is prohibited");
    public static readonly ErrorType TypedArrayInvalidIntegerIndex = new("Invalid integer index: {}");
    public static readonly ErrorType TypedArrayInvalidTargetOffset = new("Invalid target offset: must be {}");
    public static readonly ErrorType TypedArrayOutOfRangeByteOffset = new("Typed array byte offset {} is out of range for buffer with length {}");
    public static readonly ErrorType TypedArrayOutOfRangeByteOffsetOrLength = new("Typed array range {}:{} is out of range for buffer with length {}");
    public static readonly ErrorType TypedArrayOverflow = new("Overflow in {}");
    public static readonly ErrorType TypedArrayOverflowOrOutOfBounds = new("Overflow or out of bounds in {}");
    public static readonly ErrorType TypedArrayPrototypeOneArg = new("TypedArray.prototype.{}() requires at least one argument");
    public static readonly ErrorType TypedArrayTypeIsNot = new("Typed array {} element type is not {}");
    public static readonly ErrorType UnknownIdentifier = new("'{}' is not defined");
    public static readonly ErrorType UnsupportedDeleteSuperProperty = new("Can't delete a property on 'super'");
    public static readonly ErrorType URIMalformed = new("URI malformed");
    public static readonly ErrorType WrappedFunctionCallThrowCompletion = new("Call of wrapped target function did not complete normally");
    public static readonly ErrorType WrappedFunctionCopyNameAndLengthThrowCompletion = new("Trying to copy target name and length did not complete normally");
    public static readonly ErrorType YieldFromIteratorMissingThrowMethod = new("yield* protocol violation: iterator must have a throw method");
    // END GENERATED ERROR TYPES

    // C++: String message() const { return m_message; }
    public string Message => _message;

    // C++: private: explicit ErrorType(StringView message)
    private ErrorType(string message)
    {
        // C++: m_message(MUST(String::from_utf8(message)))
        // In C#, just assign the string directly.
        _message = message;
    }

    private readonly string _message;
}