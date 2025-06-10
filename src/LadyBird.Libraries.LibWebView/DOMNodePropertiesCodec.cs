// namespace LadyBird.Libraries.WebView;
//
// using System.Text;
// using AK;
//
// public static class DOMNodePropertiesCodec
// {
// // C++: template<> ErrorOr<void> encode(Encoder&, WebView::DOMNodeProperties const&);
//     public static Result<void> Encode(Encoder encoder, DOMNodeProperties attribute)
//     {
//         // C++: TRY(encoder.encode(attribute.type));
//         var typeResult = encoder.Encode(attribute.NodeType);
//         if (typeResult.IsFailure)
//             return Result<void>.Failure(typeResult.Error);
//
//         // C++: TRY(encoder.encode(attribute.properties));
//         var propertiesResult = encoder.Encode(attribute.Properties);
//         if (propertiesResult.IsFailure)
//             return Result<void>.Failure(propertiesResult.Error);
//
//         return Result<void>.Success();
//     }
//
// // C++: template<> ErrorOr<WebView::DOMNodeProperties> decode(Decoder&);
//     public static Result<DOMNodeProperties> Decode(Decoder decoder)
//     {
//         // C++: auto type = TRY(decoder.decode<WebView::DOMNodeProperties::Type>());
//         var typeResult = decoder.Decode<DOMNodeProperties.Type>();
//         if (typeResult.IsFailure)
//             return Result<DOMNodeProperties>.Failure(typeResult.Error);
//
//         // C++: auto properties = TRY(decoder.decode<JsonValue>());
//         var propertiesResult = decoder.Decode<JsonNode>();
//         if (propertiesResult.IsFailure)
//             return Result<DOMNodeProperties>.Failure(propertiesResult.Error);
//
//         // C++: return WebView::DOMNodeProperties { type, move(properties) };
//         var domNodeProperties = new DOMNodeProperties
//         {
//             NodeType = typeResult.Value,
//             Properties = propertiesResult.Value
//         };
//         return Result<DOMNodeProperties>.Success(domNodeProperties);
//     }
// }