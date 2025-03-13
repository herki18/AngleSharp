CSSColorValue 

We need to take a look how Alpha is used properly.
Test is failing because when we do this 
var fallbackValue = new CssColorValue(0, 0, 255, 1);

anc call fallbackValue.CssText it will be rgba(255, 0, 0, 0)
but should be rgba(255, 0, 0, 1)


var fn = FunctionNames.Rgba;  
var args = String.Join(", ", new[]  
{  
    R.ToString(CultureInfo.InvariantCulture),  
    G.ToString(CultureInfo.InvariantCulture),  
    B.ToString(CultureInfo.InvariantCulture),  
    A.ToString(CultureInfo.InvariantCulture),  
});  
return fn.CssFunction(args);

var fn = FunctionNames.Rgba;  
var args = String.Join(", ", new[]  
{  
    R.ToString(CultureInfo.InvariantCulture),  
    G.ToString(CultureInfo.InvariantCulture),  
    B.ToString(CultureInfo.InvariantCulture),  
    Alpha.ToString(CultureInfo.InvariantCulture),  
});  
return fn.CssFunction(args);



