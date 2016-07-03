using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HRMC
{
    public enum Token
    {
        Int,
        Symbol,
        Number,
        LessOrEqualTo,
        LessThan,
        GreaterThan,
        GreaterThanOrEqual,
        Equal, // == 
        NotEqual, // != 
        Is, // =
        If,
        While,
        Else,
        ParenOpen,
        ParenClose,
        BlockOpen,
        BlockClose,
        WhiteSpace,
        Semicolon,
        Plus,
        Minus,
        LogicalAnd, // &&
        LogicalOr, // ||
        Asterisk,
        Increment, //++
        Decrement, //--
        BracketOpen,
        BracketClose,
        True,
        False,
        Const,
        Mod,
        Div
    }

    public class TokenElement
    {
        public Token Type
        {
            get;
            set;
        }

        public string Data { get; set; }

        public TokenElement(Token type, string data = null)
        {
            Type = type;
            Data = data;
        }
    }

    public class Tokenizer
    {
        public IEnumerable<TokenElement> Lex(string program)
        {
            return Lex(GenerateStreamFromString(program));
        }

        static Stream GenerateStreamFromString(string s)
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(s));
        }

        public IEnumerable<TokenElement> Lex(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                bool lessThanSeen = false;
                bool greaterThanSeen = false;
                bool equalSeen = false;
                bool andSeen = false;
                bool orSeen = false;
                bool notSeen = false;
                bool plusSeen = false;
                bool minusSeen = false;
                bool divSeen = false;

                bool inComment = false;

                StringBuilder name = new StringBuilder();
                StringBuilder number = new StringBuilder();

                foreach (var c in GetChars(reader))
                {
                    if (inComment)
                    {
                        if (c == '\n' || c == '\r')
                        {
                            inComment = false;
                        }
                        continue;
                    }

                    if (divSeen && c != '/')
                    {
                        divSeen = false;
                        yield return new TokenElement(Token.Div);
                    }

                    if (plusSeen && c != '+')
                    {
                        plusSeen = false;
                        yield return new TokenElement(Token.Plus);
                    }

                    if (minusSeen && c != '-')
                    {
                        minusSeen = false;
                        yield return new TokenElement(Token.Minus);
                    }

                    if (equalSeen && c != '=')
                    {
                        equalSeen = false;
                        yield return new TokenElement(Token.Is);
                    }

                    if (greaterThanSeen && c != '=')
                    {
                        greaterThanSeen = false;
                        yield return new TokenElement(Token.GreaterThan);
                    }

                    if (lessThanSeen && c != '=')
                    {
                        lessThanSeen = false;
                        yield return new TokenElement(Token.LessThan);
                    }

                    if (char.IsLetter(c))
                    {
                        name.Append(c);
                        continue;
                    }

                    if (char.IsNumber(c))
                    {
                        number.Append(c);
                        continue;
                    }

                    if (number.Length > 0)
                    {
                        yield return new TokenElement(Token.Number, number.ToString());
                        number.Clear();
                    }

                    if (name.Length > 0)
                    {
                        var value = name.ToString();
                        switch (value)
                        {
                            case "int":
                                yield return new TokenElement(Token.Int);
                                break;
                            case "if":
                                yield return new TokenElement(Token.If);
                                break;
                            case "else":
                                yield return new TokenElement(Token.Else);
                                break;
                            case "while":
                                yield return new TokenElement(Token.While);
                                break;
                            case "true":
                                yield return new TokenElement(Token.True);
                                break;
                            case "false":
                                yield return new TokenElement(Token.False);
                                break;
                            case "const":
                                yield return new TokenElement(Token.Const);
                                break;
                            default:
                                yield return new TokenElement(Token.Symbol, name.ToString());
                                break;
                        }

                        name.Clear();
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    switch (c)
                    {
                        case '+':
                            if (plusSeen)
                            {
                                yield return new TokenElement(Token.Increment);
                            }
                            else
                            {
                                plusSeen = true;
                                continue;
                            }
                            break;
                        case '-':
                            if (minusSeen)
                            {
                                yield return new TokenElement(Token.Decrement);
                            }
                            else
                            {
                                minusSeen = true;
                                continue;
                            }
                            break;
                        case '*':
                            yield return new TokenElement(Token.Asterisk);
                            break;
                        case '%':
                            yield return new TokenElement(Token.Mod);
                            break;
                        case ';':
                            yield return new TokenElement(Token.Semicolon);
                            break;
                        case '{':
                            yield return new TokenElement(Token.BlockOpen);
                            break;
                        case '}':
                            yield return new TokenElement(Token.BlockClose);
                            break;
                        case '[':
                            yield return new TokenElement(Token.BracketOpen);
                            break;
                        case ']':
                            yield return new TokenElement(Token.BracketClose);
                            break;
                        case '(':
                            yield return new TokenElement(Token.ParenOpen);
                            break;
                        case ')':
                            yield return new TokenElement(Token.ParenClose);
                            break;
                        case '<':
                            lessThanSeen = true;
                            continue;
                        case '>':
                            greaterThanSeen = true;
                            continue;
                        case '!':
                            notSeen = true;
                            continue;
                        case '/':
                            if (divSeen)
                            {
                                inComment = true;
                                divSeen = false;
                            }
                            else
                            {
                                divSeen = true;
                            }
                            continue;

                        case '&':
                            if (andSeen)
                            {
                                yield return new TokenElement(Token.LogicalAnd);
                            }
                            else
                            {
                                andSeen = true;
                                continue;
                            }
                            break;
                        case '|':
                            if (orSeen)
                            {
                                yield return new TokenElement(Token.LogicalOr);
                            }
                            else
                            {
                                orSeen = true;
                                continue;
                            }
                            break;

                        case '=':
                            if (lessThanSeen)
                            {
                                yield return new TokenElement(Token.LessOrEqualTo);
                            }
                            else if (greaterThanSeen)
                            {
                                yield return new TokenElement(Token.GreaterThanOrEqual);
                            }
                            else if (equalSeen)
                            {
                                yield return new TokenElement(Token.Equal);
                            }
                            else if (notSeen)
                            {
                                yield return new TokenElement(Token.NotEqual);
                            }
                            else
                            {
                                equalSeen = true;
                                continue;
                            }
                            break;
                        default:
                            throw new Exception("Unknown token " + c);
                    }

                    lessThanSeen =
                        greaterThanSeen =
                        equalSeen = 
                        andSeen = 
                        orSeen =
                        notSeen = 
                        plusSeen = 
                        minusSeen = 
                        divSeen = false;
                }
            }
        }

        IEnumerable<char> GetChars(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                line = line.Trim();

                foreach (var c in line)
                {
                    yield return c;
                }
                yield return '\n';
            }
        }
    }
}