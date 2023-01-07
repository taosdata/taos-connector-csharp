using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos
{
    enum StatementState
    {
        None = 0,
        Body = 1,
        Quotation = 2,
        Parameter = 3,
        ShortComment = 4,
        Comment = 5,
    }

    internal abstract class StatementPart
    {
        protected readonly string _Text;
        protected StatementPart(string text)
        {
            _Text = text;
        }
        public string OriginalText => _Text;
        public virtual string GetText() => _Text;
    }

    internal sealed class Body : StatementPart
    {
        public Body(string text) : base(text)
        {
        }
    }

    internal sealed class Literal : StatementPart
    {
        public Literal(string text) : base(text)
        {
        }
    }

    internal sealed class Comment : StatementPart
    {
        public Comment(string text) : base(text)
        {
        }
        public override string GetText() => " ";
    }

    internal sealed class Param : StatementPart
    {
        public Param(string text) : base(text)
        {
        }
        public override string GetText() => "?";
    }

    internal sealed class StatementObject : IReadOnlyCollection<StatementPart>
    {
        private readonly StatementPart[] _Parts;
        private StatementObject(IEnumerable<StatementPart> parts)
        {
            _Parts = parts.ToArray();
        }

        public string[] ParameterNames => _Parts.OfType<Param>().Where(p => p.OriginalText.StartsWith("@")).Select(p => p.OriginalText).ToArray();

        public string CommandText => string.Join(string.Empty, _Parts.Select(p => p.GetText()));

        public int Count => _Parts.Length;

        public string SubTableName => _Parts.OfType<Param>().Where(p => p.OriginalText.StartsWith("#")).Select(p => p.OriginalText).ToList().FirstOrDefault();
        public string[] TagsNames => _Parts.OfType<Param>().Where(p => p.OriginalText.StartsWith("$")).Select(p => p.OriginalText).ToArray();

        public StatementPart this[int index] => _Parts[index];

        public IEnumerator<StatementPart> GetEnumerator()
        {
            return (_Parts as IEnumerable<StatementPart>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Parts.GetEnumerator();
        }

        public static StatementObject ResolveCommandText(string commandText)
        {
            return new StatementObject(Resolve(commandText));
        }

        private static bool TryGetChar(string statement, int pos, out char ch)
        {
            if (statement.Length > pos)
            {
                ch = statement[pos];
                return true;
            }
            ch = char.MinValue;
            return false;
        }

        private static IEnumerable<StatementPart> Resolve(string statement)
        {
            var status = StatementState.None;
            var pos = 0;
            var partStart = 0;
            while (TryGetChar(statement, pos, out char ch))
            {
                if (status == StatementState.None)
                {
                    status = StatementState.Body;
                }
                if (status == StatementState.Body)
                {
                    // 检测 ' @ -- /*
                    if (ch == '\'')
                    {
                        if (pos > partStart)
                        {
                            yield return new Body(statement.Substring(partStart, pos - partStart));
                            partStart = pos;
                        }
                        status = StatementState.Quotation;
                    }
                    else if (ch == '@' || ch == '$' || ch == '#')
                    {
                        if (pos > partStart)
                        {
                            yield return new Body(statement.Substring(partStart, pos - partStart));
                            partStart = pos;
                        }
                        status = StatementState.Parameter;
                    }
                    else if (ch == '-')
                    {
                        if (TryGetChar(statement, pos + 1, out char x))
                        {
                            if (x == '-')
                            {
                                if (pos > partStart)
                                {
                                    yield return new Body(statement.Substring(partStart, pos - partStart));
                                    partStart = pos;
                                }
                                status = StatementState.ShortComment;
                                partStart = pos;
                                pos++;
                            }
                        }
                    }
                    else if (ch == '/')
                    {
                        if (TryGetChar(statement, pos + 1, out char x))
                        {
                            if (x == '*')
                            {
                                if (pos > partStart)
                                {
                                    yield return new Body(statement.Substring(partStart, pos - partStart));
                                    partStart = pos;
                                }
                                status = StatementState.Comment;
                                partStart = pos;
                                pos++;
                            }
                        }
                    }
                }
                else if (status == StatementState.Quotation)
                {
                    // 检测 ' \'
                    if (ch == '\'')
                    {
                        yield return new Literal(statement.Substring(partStart, pos - partStart + 1));
                        pos++;
                        partStart = pos;
                        status = StatementState.Body;
                    }
                    else if (ch == '\\')
                    {
                        if (TryGetChar(statement, pos + 1, out char x))
                        {
                            if (x == '\'')
                            {
                                pos++;
                            }
                        }
                    }
                }
                else if (status == StatementState.Parameter)
                {
                    // 检测 [^a-z0-9_]
                    if (char.IsLetter(ch) || char.IsDigit(ch) || ch == '_')
                    {

                    }
                    else
                    {
                        yield return new Param(statement.Substring(partStart, pos - partStart));
                        status = StatementState.Body;
                        partStart = pos;
                    }
                }
                else if (status == StatementState.Comment)
                {
                    // 检测 */
                    if (ch == '*')
                    {
                        if (TryGetChar(statement, pos + 1, out char x))
                        {
                            if (x == '/')
                            {
                                yield return new Comment(statement.Substring(partStart, pos - partStart + 2));
                                status = StatementState.Body;
                                pos += 2;
                                partStart = pos;
                            }
                        }
                    }
                }
                pos++;
            }
            if (pos > partStart)
            {
                if (status == StatementState.Body)
                {
                    yield return new Body(statement.Substring(partStart, pos - partStart));
                }
                else if (status == StatementState.ShortComment)
                {
                    yield return new Comment(statement.Substring(partStart, pos - partStart));
                }
                else if (status == StatementState.Parameter)
                {
                    yield return new Param(statement.Substring(partStart, pos - partStart));
                }
                else if (status == StatementState.Quotation)
                {
                    throw new Exception($"字面字符串未关闭：{statement}");
                }
                else if (status == StatementState.Comment)
                {
                    throw new Exception($"内嵌式注释未关闭：{statement}");
                }
            }
        }
    }

}
