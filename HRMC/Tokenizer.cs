using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public int Line { get; set; }
        public int Column { get; set; }

        public TokenElement(Token type, Character character, string data = null)
        {
            Type = type;
            Data = data;
            Line = character.line;
            Column = character.column;
        }
    }

    public struct Character
    {
        public char c;
        public int line;
        public int column;
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

                foreach (var ch in GetChars(reader))
                {
                    var c = ch.c;

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
                        yield return new TokenElement(Token.Div, ch);
                    }

                    if (plusSeen && c != '+')
                    {
                        plusSeen = false;
                        yield return new TokenElement(Token.Plus, ch);
                    }

                    if (minusSeen && c != '-')
                    {
                        minusSeen = false;
                        yield return new TokenElement(Token.Minus, ch);
                    }

                    if (equalSeen && c != '=')
                    {
                        equalSeen = false;
                        yield return new TokenElement(Token.Is, ch);
                    }

                    if (greaterThanSeen && c != '=')
                    {
                        greaterThanSeen = false;
                        yield return new TokenElement(Token.GreaterThan, ch);
                    }

                    if (lessThanSeen && c != '=')
                    {
                        lessThanSeen = false;
                        yield return new TokenElement(Token.LessThan, ch);
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
                        yield return new TokenElement(Token.Number, ch, number.ToString());
                        number.Clear();
                    }

                    if (name.Length > 0)
                    {
                        var value = name.ToString();
                        switch (value)
                        {
                            case "int":
                                yield return new TokenElement(Token.Int, ch);
                                break;
                            case "if":
                                yield return new TokenElement(Token.If, ch);
                                break;
                            case "else":
                                yield return new TokenElement(Token.Else, ch);
                                break;
                            case "while":
                                yield return new TokenElement(Token.While, ch);
                                break;
                            case "true":
                                yield return new TokenElement(Token.True, ch);
                                break;
                            case "false":
                                yield return new TokenElement(Token.False, ch);
                                break;
                            case "const":
                                yield return new TokenElement(Token.Const, ch);
                                break;
                            default:
                                yield return new TokenElement(Token.Symbol, ch, name.ToString());
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
                                yield return new TokenElement(Token.Increment, ch);
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
                                yield return new TokenElement(Token.Decrement, ch);
                            }
                            else
                            {
                                minusSeen = true;
                                continue;
                            }
                            break;
                        case '*':
                            yield return new TokenElement(Token.Asterisk, ch);
                            break;
                        case '%':
                            yield return new TokenElement(Token.Mod, ch);
                            break;
                        case ';':
                            yield return new TokenElement(Token.Semicolon, ch);
                            break;
                        case '{':
                            yield return new TokenElement(Token.BlockOpen, ch);
                            break;
                        case '}':
                            yield return new TokenElement(Token.BlockClose, ch);
                            break;
                        case '[':
                            yield return new TokenElement(Token.BracketOpen, ch);
                            break;
                        case ']':
                            yield return new TokenElement(Token.BracketClose, ch);
                            break;
                        case '(':
                            yield return new TokenElement(Token.ParenOpen, ch);
                            break;
                        case ')':
                            yield return new TokenElement(Token.ParenClose, ch);
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
                                yield return new TokenElement(Token.LogicalAnd, ch);
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
                                yield return new TokenElement(Token.LogicalOr, ch);
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
                                yield return new TokenElement(Token.LessOrEqualTo, ch);
                            }
                            else if (greaterThanSeen)
                            {
                                yield return new TokenElement(Token.GreaterThanOrEqual, ch);
                            }
                            else if (equalSeen)
                            {
                                yield return new TokenElement(Token.Equal, ch);
                            }
                            else if (notSeen)
                            {
                                yield return new TokenElement(Token.NotEqual, ch);
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

        IEnumerable<Character> GetChars(StreamReader reader)
        {
            int lineCount = 1;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                line+="\n";

                foreach (var c in line.Select((cc, i) => new Character { c = cc, column = i+1, line = lineCount }))
                {
                    yield return c;
                }
                lineCount++;
            }
        }
    }
}