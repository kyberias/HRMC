﻿using System;
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
        Equals, // == 
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
        LogicalOr, // &&
        Asterisk,
        Increment, //++
        Decrement, //--
        BracketOpen,
        BracketClose
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
                bool equalSeen = false;
                bool andSeen = false;
                bool orSeen = false;
                StringBuilder name = new StringBuilder();
                StringBuilder number = new StringBuilder();

                foreach (var c in GetChars(reader))
                {
                    if (equalSeen && c != '=')
                    {
                        equalSeen = false;
                        yield return new TokenElement(Token.Is);
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
                            yield return new TokenElement(Token.Plus);
                            break;
                        case '-':
                            yield return new TokenElement(Token.Minus);
                            break;
                        case '*':
                            yield return new TokenElement(Token.Asterisk);
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
                            else if (equalSeen)
                            {
                                yield return new TokenElement(Token.Equals);
                            }
                            else
                            {
                                equalSeen = true;
                                continue;
                                //yield return new TokenElement(HrmLexType.Is);
                            }
                            break;
                        default:
                            throw new Exception("Unknown token " + c);
                    }

                    lessThanSeen = 
                        equalSeen = 
                        andSeen = 
                        orSeen = false;
                }
            }
        }

        IEnumerable<char> GetChars(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                line = line.Trim();

                if (line.Length > 1 && line.StartsWith("//"))
                {
                    continue;
                }

                foreach (var c in line)
                {
                    yield return c;
                }
            }
        }
    }
}