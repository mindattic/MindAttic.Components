using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T185: MaShapeBorder.ClipToShape clips the child to the frame's outline.</summary>
public sealed class MaShapeBorderClipTests
{
    [Fact]
    public void A_chamfer_clips_its_child_to_the_cut_corners() => StaHost.Run(h =>
    {
        var child = new Border { Background = Brushes.Red };
        var frame = new MaShapeBorder { Shape = MaShape.Chamfer, Chamfer = 20, ClipToShape = true, Width = 200, Height = 100, Child = child };
        h.Show(frame);

        Assert.NotNull(child.Clip);
        Assert.False(child.Clip!.FillContains(new Point(2, 2)), "the top-left cut is outside the clip");
        Assert.False(child.Clip.FillContains(new Point(198, 98)), "the bottom-right cut is outside the clip");
        Assert.True(child.Clip.FillContains(new Point(100, 50)));
        Assert.True(child.Clip.FillContains(new Point(198, 2)), "the top-right corner is square");
    });

    [Fact]
    public void Padding_shifts_the_clip_into_the_childs_coordinates() => StaHost.Run(h =>
    {
        var child = new Border();
        var frame = new MaShapeBorder { Shape = MaShape.Chamfer, Chamfer = 20, ClipToShape = true, Padding = new Thickness(10), Width = 200, Height = 100, Child = child };
        h.Show(frame);

        // The child starts 10 px in, so the frame's (12,12), inside the cut's reach, is the child's (2,2).
        Assert.False(child.Clip!.FillContains(new Point(2, 2)));
        Assert.True(child.Clip.FillContains(new Point(90, 40)));
    });

    [Fact]
    public void The_clip_follows_a_new_chamfer_and_leaves_a_swapped_out_child() => StaHost.Run(h =>
    {
        var child = new Border();
        var frame = new MaShapeBorder { Shape = MaShape.Chamfer, Chamfer = 10, ClipToShape = true, Width = 200, Height = 100, Child = child };
        h.Show(frame);
        Assert.True(child.Clip!.FillContains(new Point(12, 2)), "inside a 10 px cut's reach");

        frame.Chamfer = 30;
        frame.UpdateLayout();
        Assert.False(child.Clip!.FillContains(new Point(12, 2)), "now inside the deeper 30 px cut");

        frame.Child = new Border();
        frame.UpdateLayout();
        Assert.Null(child.Clip);
    });

    [Fact]
    public void Off_by_default_and_turning_it_off_removes_only_its_own_clip() => StaHost.Run(h =>
    {
        var child = new Border();
        var frame = new MaShapeBorder { Shape = MaShape.Chamfer, Width = 200, Height = 100, Child = child };
        h.Show(frame);
        Assert.Null(child.Clip);

        frame.ClipToShape = true;
        frame.UpdateLayout();
        Assert.NotNull(child.Clip);

        frame.ClipToShape = false;
        frame.UpdateLayout();
        Assert.Null(child.Clip);

        var own = new RectangleGeometry(new Rect(0, 0, 5, 5));
        child.Clip = own;
        frame.InvalidateArrange();
        frame.UpdateLayout();
        Assert.Same(own, child.Clip);
    });
}
