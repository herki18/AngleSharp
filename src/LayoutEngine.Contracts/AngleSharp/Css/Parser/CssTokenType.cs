namespace AngleSharp.Css.Parser;

/// <summary>
/// An enumeration of all possible tokens.
/// </summary>
public enum CssTokenType : byte
{
    /// <summary>
    /// A string token (usually in quotation marks).
    /// </summary>
    String,
    /// <summary>
    /// A URL token.
    /// </summary>
    Url,
    /// <summary>
    /// A color token.
    /// </summary>
    Color,
    /// <summary>
    /// A hash token (starts with #).
    /// </summary>
    Hash,
    /// <summary>
    /// A comment token (/*...*/).
    /// </summary>
    Comment,
    /// <summary>
    /// An @-keyword token (starts with @).
    /// </summary>
    AtKeyword,
    /// <summary>
    /// A class token (starts with .).
    /// </summary>
    Class,
    /// <summary>
    /// An identifier token.
    /// </summary>
    Ident,
    /// <summary>
    /// An function token.
    /// </summary>
    Function,
    /// <summary>
    /// An number token.
    /// </summary>
    Number,
    /// <summary>
    /// An percentage token.
    /// </summary>
    Percentage,
    /// <summary>
    /// An dimension token.
    /// </summary>
    Dimension,
    /// <summary>
    /// An unicode range token.
    /// </summary>
    Range,
    /// <summary>
    /// The comment open token to start comments.
    /// </summary>
    Cdo,
    /// <summary>
    /// The comment close to end comments.
    /// </summary>
    Cdc,
    /// <summary>
    /// The column ( || ) token.
    /// </summary>
    Column,
    /// <summary>
    /// The descendant ( >> ) token.
    /// </summary>
    Descendant,
    /// <summary>
    /// The deep ( >>> ) token.
    /// </summary>
    Deep,
    /// <summary>
    /// The delimiter token to delimiter character.
    /// </summary>
    Delim,
    /// <summary>
    /// The match token (~=, |=, $=, ^=, *=, or !=).
    /// </summary>
    Match,
    /// <summary>
    /// The RoundBracketOpen ( ( ) token.
    /// </summary>
    RoundBracketOpen,
    /// <summary>
    /// The RoundBracketClose ( ) ) token.
    /// </summary>
    RoundBracketClose,
    /// <summary>
    /// The CurlyBracketOpen ( { ) token.
    /// </summary>
    CurlyBracketOpen,
    /// <summary>
    /// The CurlyBracketClose ( } ) token.
    /// </summary>
    CurlyBracketClose,
    /// <summary>
    /// The SquareBracketOpen ( [ ) token.
    /// </summary>
    SquareBracketOpen,
    /// <summary>
    /// The SquareBracketClose ( ] ) token.
    /// </summary>
    SquareBracketClose,
    /// <summary>
    /// The special character colon ( : ).
    /// </summary>
    Colon,
    /// <summary>
    /// The special character comma ( , ).
    /// </summary>
    Comma,
    /// <summary>
    /// The special character semi-colon ( ; ).
    /// </summary>
    Semicolon,
    /// <summary>
    /// The special character whitespace ( ).
    /// </summary>
    Whitespace,
    /// <summary>
    /// The invalid token (any).
    /// </summary>
    Invalid,
    /// <summary>
    /// The end-of-file marker.
    /// </summary>
    EndOfFile,
}