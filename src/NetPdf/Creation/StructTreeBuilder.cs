using System.Text;
using System.Text.RegularExpressions;
using NetPdf.Layout;
using PdfSharp.Pdf;
using SharpDocument = PdfSharp.Pdf.PdfDocument;

namespace NetPdf.Creation;

/// <summary>
/// Post-pass run before saving a tagged document: replaces the sentinel comments emitted
/// through <c>XGraphics.WriteComment</c> with real BDC/EMC marked-content operators, and
/// builds the /StructTreeRoot, /ParentTree, and /MarkInfo entries from the recorded
/// tagging session.
/// </summary>
internal static partial class StructTreeBuilder
{
    [GeneratedRegex(@"%\s*MCB (\d+) (\w+)\s*")]
    private static partial Regex BeginSentinelRegex();

    [GeneratedRegex(@"%\s*MCE\s*")]
    private static partial Regex EndSentinelRegex();

    internal static void Apply(SharpDocument document, TaggingSession session)
    {
        if (session.Roots.Count == 0)
            return;

        foreach (var page in session.Pages)
            RewriteContentStreams(page);

        // Structure elements. Every recorded entry becomes a StructElem; MCIDs are its /K.
        var structTreeRoot = new PdfDictionary(document);
        document.Internals.AddObject(structTreeRoot);
        structTreeRoot.Elements.SetName("/Type", "/StructTreeRoot");

        var documentElem = new PdfDictionary(document);
        document.Internals.AddObject(documentElem);
        documentElem.Elements.SetName("/Type", "/StructElem");
        documentElem.Elements.SetName("/S", "/Document");
        documentElem.Elements["/P"] = structTreeRoot.Reference;
        var documentKids = new PdfArray(document);
        documentElem.Elements["/K"] = documentKids;

        // Per page: MCID → owning structure element, for the parent tree.
        var mcidOwners = new Dictionary<PdfPage, SortedDictionary<int, PdfDictionary>>();

        void Build(TagEntry entry, PdfDictionary parent, PdfArray parentKids)
        {
            var elem = new PdfDictionary(document);
            document.Internals.AddObject(elem);
            elem.Elements.SetName("/Type", "/StructElem");
            elem.Elements.SetName("/S", "/" + TaggingSession.StructureType(entry.Role));
            elem.Elements["/P"] = parent.Reference;
            elem.Elements["/Pg"] = entry.Page.Reference;
            if (entry.AltText is { } alt)
                elem.Elements.SetString("/Alt", alt);

            var kids = new PdfArray(document);
            kids.Elements.Add(new PdfInteger(entry.Mcid));
            if (!mcidOwners.TryGetValue(entry.Page, out var owners))
                mcidOwners[entry.Page] = owners = [];
            owners[entry.Mcid] = elem;

            foreach (var child in entry.Children)
                Build(child, elem, kids);

            elem.Elements["/K"] = kids.Elements.Count == 1 ? kids.Elements[0] : kids;
            parentKids.Elements.Add(elem.Reference!);
        }

        foreach (var root in session.Roots)
            Build(root, documentElem, documentKids);

        // Parent tree: /StructParents index per page → array indexed by MCID.
        var nums = new PdfArray(document);
        var key = 0;
        foreach (var page in session.Pages)
        {
            var owners = mcidOwners[page];
            var arr = new PdfArray(document);
            document.Internals.AddObject(arr);
            var maxMcid = owners.Keys.Max();
            for (var mcid = 0; mcid <= maxMcid; mcid++)
                arr.Elements.Add(owners.TryGetValue(mcid, out var owner)
                    ? owner.Reference!
                    : PdfNull.Value);
            nums.Elements.Add(new PdfInteger(key));
            nums.Elements.Add(arr.Reference!);
            page.Elements.SetInteger("/StructParents", key);
            page.Elements.SetName("/Tabs", "/S");
            key++;
        }

        var parentTree = new PdfDictionary(document);
        document.Internals.AddObject(parentTree);
        parentTree.Elements["/Nums"] = nums;

        structTreeRoot.Elements["/K"] = documentElem.Reference;
        structTreeRoot.Elements["/ParentTree"] = parentTree.Reference;
        structTreeRoot.Elements.SetInteger("/ParentTreeNextKey", key);

        var markInfo = new PdfDictionary(document);
        markInfo.Elements.SetBoolean("/Marked", true);
        document.Internals.Catalog.Elements["/MarkInfo"] = markInfo;
        document.Internals.Catalog.Elements["/StructTreeRoot"] = structTreeRoot.Reference;
    }

    /// <summary>Replaces MCB/MCE sentinel comments with BDC/EMC operators in a page's content streams.</summary>
    private static void RewriteContentStreams(PdfPage page)
    {
        for (var i = 0; i < page.Contents.Elements.Count; i++)
        {
            var content = page.Contents.Elements.GetDictionary(i);
            if (content?.Stream is null)
                continue;
            content.Stream.TryUnfilter();
            var text = Encoding.Latin1.GetString(content.Stream.Value);
            if (!text.Contains("MCB", StringComparison.Ordinal))
                continue;
            text = BeginSentinelRegex().Replace(text, m => $"/{m.Groups[2].Value} <</MCID {m.Groups[1].Value}>> BDC\n");
            text = EndSentinelRegex().Replace(text, "EMC\n");
            content.Stream.Value = Encoding.Latin1.GetBytes(text);
        }
    }
}
