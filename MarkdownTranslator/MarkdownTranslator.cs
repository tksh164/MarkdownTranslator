using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MDT = Markdig.Extensions.Tables;

namespace MarkdownTranslator
{
    internal sealed class MarkdownTranslator
    {
        private MarkdownTranslator()
        {
        }

        public static async Task TranslateToWriter(string sourceFilePath, TextWriter writer)
        {
            var mdDoc = ParseMarkdownDocument(sourceFilePath);
            foreach (var block in mdDoc)
            {
                await writer.WriteLineAsync(TranslateBlock(block, 0));
            }
        }

        private static MarkdownDocument ParseMarkdownDocument(string markdownFilePath)
        {
            var markdownText = File.ReadAllText(markdownFilePath);
            var pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
            return Markdown.Parse(markdownText, pipeline);
        }

        private static string TranslateBlock(Block block, int nestLevel)
        {
            var translatedBlockText = new StringBuilder();

            switch (block)
            {
                case ParagraphBlock paragraphBlock:
                    translatedBlockText.AppendLine(TranslateParagraphBlock(paragraphBlock, nestLevel));
                    break;

                case HeadingBlock headingBlock:
                    translatedBlockText.AppendLine(TranslateHeadingBlock(headingBlock, nestLevel));
                    break;

                case ListBlock listBlock:
                    translatedBlockText.AppendLine(TranslateListBlock(listBlock, nestLevel));
                    break;

                case CodeBlock codeBlock:
                    translatedBlockText.AppendLine(TranslateCodeBlock(codeBlock, nestLevel));
                    break;

                case MDT.Table tableBlock:
                    translatedBlockText.AppendLine(TranslateTableBlock(tableBlock, nestLevel));
                    break;

                //case QuoteBlock quoteBlock:
                //    break;

                //case HtmlBlock htmlBlock:
                //    break;

                default:
                    throw new NotImplementedException(string.Format("Unknown block type: {0}", block.GetType().FullName));
            }

            return translatedBlockText.ToString();
        }

        private static string GetIndentWhitespaces(int nestLevel)
        {
            const int numOfSpacesForIndent = 4;
            return new string(' ', numOfSpacesForIndent * nestLevel);
        }

        private static string TranslateParagraphBlock(ParagraphBlock paragraphBlock, int nestLevel)
        {
            string paragraphBlockText;

            (var plainInlineText, var shouldBeTranslate) = GetTextToTranslate(paragraphBlock.Inline);
            if (shouldBeTranslate)
            {
                var result = TranslatorClient.Translate(plainInlineText, "ja", "en");
                paragraphBlockText = result.Result;
            }
            else
            {
                paragraphBlockText = plainInlineText;
            }

            return GetIndentWhitespaces(nestLevel) + paragraphBlockText;
        }

        private static string TranslateHeadingBlock(HeadingBlock headingBlock, int nestLevel)
        {
            var headingLevelMarkText = new string('#', headingBlock.Level);

            string headingBlockText;

            (var plainInlineText, var shouldBeTranslate) = GetTextToTranslate(headingBlock.Inline);
            if (shouldBeTranslate)
            {
                var result = TranslatorClient.Translate(plainInlineText, "ja", "en");
                headingBlockText = string.Format("{0} {1}", headingLevelMarkText, result.Result);
            }
            else
            {
                headingBlockText = string.Format("{0} {1}", headingLevelMarkText, plainInlineText);
            }

            return GetIndentWhitespaces(nestLevel) + headingBlockText;
        }

        private static string TranslateListBlock(ListBlock listBlock, int nestLevel)
        {
            var bulletNumber = listBlock.BulletType == '1' ? int.Parse(listBlock.OrderedStart) : 0;

            var transltedText = new StringBuilder();

            foreach (ListItemBlock listItemBlock in listBlock)
            {
                if (listItemBlock.Count > 0)
                {
                    // The paragraph of the list item itself.
                    if (listBlock.BulletType == '1')
                    {
                        transltedText.AppendLine(string.Format("{0}. {1}", bulletNumber, TranslateListItemBlock(listItemBlock, nestLevel)));
                        bulletNumber++;
                    }
                    else
                    {
                        transltedText.AppendLine(string.Format("{0} {1}", listBlock.BulletType, TranslateListItemBlock(listItemBlock, nestLevel)));
                    }

                    transltedText.AppendLine();

                    // The nedsted blocks.
                    for (int i = 1; i < listItemBlock.Count; i++)
                    {
                        if (listItemBlock[i].GetType() == typeof(ListBlock))
                        {
                            transltedText.AppendLine(TranslateBlock(listItemBlock[i], nestLevel + 1));
                        }
                        else
                        {
                            transltedText.AppendLine(TranslateBlock(listItemBlock[i], nestLevel + 1));
                        }
                    }
                }
            }

            return GetIndentWhitespaces(nestLevel) + transltedText.ToString();
        }

        private static string TranslateListItemBlock(ListItemBlock listItemBlock, int nestLevel)
        {
            return TranslateParagraphBlock((ParagraphBlock)listItemBlock[0], nestLevel);
        }

        private static string TranslateCodeBlock(CodeBlock codeBlock, int nestLevel)
        {
            var codeBlockText = new StringBuilder();

            if (codeBlock.GetType() == typeof(FencedCodeBlock))
            {
                // For the code block starting with ``` line and ending with ``` line.
                codeBlockText.AppendLine(GetIndentWhitespaces(nestLevel) + "````");
            }

            for (int i = 0; i < codeBlock.Lines.Count; i++)
            {
                var lineText = codeBlock.Lines.Lines[i].Slice.ToString();
                codeBlockText.AppendLine(GetIndentWhitespaces(nestLevel) + lineText);
            }

            if (codeBlock.GetType() == typeof(FencedCodeBlock))
            {
                // For the code block starting with ``` line and ending with ``` line.
                codeBlockText.AppendLine(GetIndentWhitespaces(nestLevel) + "````");
            }

            return codeBlockText.ToString();
        }

        private static string TranslateTableBlock(MDT.Table tableBlock, int nestLevel)
        {
            var tableBlockText = new StringBuilder();

            foreach (MDT.TableRow tableRow in tableBlock)
            {
                var cellTexts = new List<string>();

                foreach (MDT.TableCell tableCell in tableRow)
                {
                    var cellText = new StringBuilder();

                    foreach (ParagraphBlock paragraphBlock in tableCell)
                    {
                        cellText.Append(TranslateParagraphBlock(paragraphBlock, nestLevel));
                    }

                    cellTexts.Add(cellText.ToString());
                }

                tableBlockText.AppendLine(GetIndentWhitespaces(nestLevel) + "| " + string.Join(" | ", cellTexts.ToArray()) + " |");

                if (tableRow.IsHeader)
                {
                    var headerSeparator = new List<string>();
                    for (int i = 0; i < cellTexts.Count; i++)
                    {
                        headerSeparator.Add("--------");
                    }
                    tableBlockText.AppendLine(GetIndentWhitespaces(nestLevel) + "| " + string.Join(" | ", headerSeparator.ToArray()) + " |");
                }
            }

            return tableBlockText.ToString();
        }

        private static (string plainInlineText, bool shouldBeTranslate) GetTextToTranslate(ContainerInline containerInline)
        {
            var numOfInlineContained = containerInline.Count();
            var needOriginalMarkdown = numOfInlineContained == 1;

            var plainInlineTextBuilder = new StringBuilder();
            var shouldBeTranslate = true;

            foreach (var inline in containerInline)
            {
                switch (inline)
                {
                    case LiteralInline literalInline:
                        plainInlineTextBuilder.Append(GetTextToTranslateFromLiteralInline(literalInline));
                        break;

                    case EmphasisInline emphasisInline:
                        plainInlineTextBuilder.Append(GetTextToTranslateFromEmphasisInline(emphasisInline));
                        break;

                    case LinkInline linkInline:
                        plainInlineTextBuilder.Append(GetTextToTranslateFromLinkInline(linkInline, needOriginalMarkdown));
                        if (needOriginalMarkdown)
                        {
                            shouldBeTranslate = false;  // Should be not translate if only one LinkInline contained.
                        }
                        break;

                    case CodeInline codeInline:
                        plainInlineTextBuilder.Append(codeInline.Content.ToString());
                        if (needOriginalMarkdown)
                        {
                            shouldBeTranslate = false;  // Should be not translate if only one CodeInline contained.
                        }
                        break;

                    case LineBreakInline lineBreakInline:
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unknown inline type: {0}", inline.GetType().FullName));
                }
            }

            var plainInlineText = plainInlineTextBuilder.ToString();
            return (plainInlineText, shouldBeTranslate);
        }

        private static string GetTextToTranslateFromLiteralInline(LiteralInline literalInline)
        {
            return literalInline.Content.ToString();
        }

        private static string GetTextToTranslateFromEmphasisInline(EmphasisInline parentInline)
        {
            var plainInlineText = new StringBuilder();

            foreach (var inline in parentInline)
            {
                switch (inline)
                {
                    case LiteralInline literalInline:
                        plainInlineText.Append(GetTextToTranslateFromLiteralInline(literalInline));
                        break;

                    case EmphasisInline emphasisInline:
                        plainInlineText.Append(GetTextToTranslateFromEmphasisInline(emphasisInline));
                        break;

                    case LinkInline linkInline:
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unknown inline type: {0}", inline.GetType().FullName));
                }
            }

            return plainInlineText.ToString();
        }

        private static string GetTextToTranslateFromLinkInline(LinkInline linkInline, bool needOriginalMarkdown)
        {
            var plainInlineText = new StringBuilder();

            foreach (var inline in linkInline)
            {
                switch (inline)
                {
                    case LiteralInline literalInline:
                        plainInlineText.Append(GetTextToTranslateFromLiteralInline(literalInline));
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unknown inline type: {0}", inline.GetType().FullName));
                }
            }

            if (needOriginalMarkdown)
            {
                return string.Format("{0}[{1}]({2})", linkInline.IsImage ? "!" : "", plainInlineText.ToString(), linkInline.Url);
            }
            else
            {
                return plainInlineText.ToString();
            }
        }
    }
}
