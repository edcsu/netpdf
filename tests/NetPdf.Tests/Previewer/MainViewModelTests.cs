#if NET10_0_OR_GREATER
using NetPdf.Fluent;
using NetPdf.Previewer.App.ViewModels;
using Xunit;

namespace NetPdf.Tests.Previewer;

public class MainViewModelTests
{
    private static byte[] CreatePdf(int pages = 2)
    {
        return Document.Create(c =>
        {
            for (var i = 0; i < pages; i++)
            {
                c.Page(p => p.Content(content => content.Text($"Page {i + 1}")));
            }
        }).ToBytes();
    }

    [Fact]
    public void LoadDocument_PopulatesOnePageImagePerPage()
    {
        var vm = new MainViewModel();

        vm.LoadDocument(CreatePdf(pages: 3));

        Assert.True(vm.HasDocument);
        Assert.Equal(3, vm.Pages.Count);
        Assert.All(vm.Pages, page =>
        {
            Assert.NotEmpty(page.ImageBytes);
            Assert.NotEmpty(page.ThumbnailBytes);
        });
        Assert.Equal([1, 2, 3], vm.Pages.Select(p => p.PageNumber));
        Assert.Contains("Updated", vm.Status);
    }

    [Fact]
    public void LoadDocument_ClearsPreviousError()
    {
        var vm = new MainViewModel();
        vm.ShowError("boom");

        vm.LoadDocument(CreatePdf());

        Assert.Null(vm.ErrorText);
    }

    [Fact]
    public void ShowError_SetsErrorTextAndKeepsPages()
    {
        var vm = new MainViewModel();
        vm.LoadDocument(CreatePdf(pages: 2));

        vm.ShowError("layout overflow");

        Assert.Equal("layout overflow", vm.ErrorText);
        Assert.Equal(2, vm.Pages.Count);
    }

    [Fact]
    public void Save_WritesLastReceivedBytes()
    {
        var vm = new MainViewModel();
        var pdf = CreatePdf();
        vm.LoadDocument(pdf);
        var path = Path.Combine(Path.GetTempPath(), $"netpdf-vm-test-{Guid.NewGuid():N}.pdf");

        try
        {
            vm.Save(path);
            Assert.Equal(pdf, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Zoom_ClampsAndRerenders()
    {
        var vm = new MainViewModel();
        vm.LoadDocument(CreatePdf(pages: 1));
        var sizeAt100 = vm.Pages[0].ImageBytes.Length;

        vm.ZoomInCommand.Execute(null);
        Assert.True(vm.Zoom > 1.0);
        Assert.NotEqual(sizeAt100, vm.Pages[0].ImageBytes.Length);

        for (var i = 0; i < 30; i++)
        {
            vm.ZoomInCommand.Execute(null);
        }

        Assert.Equal(4.0, vm.Zoom);

        for (var i = 0; i < 60; i++)
        {
            vm.ZoomOutCommand.Execute(null);
        }

        Assert.Equal(0.25, vm.Zoom);
    }
}
#endif
