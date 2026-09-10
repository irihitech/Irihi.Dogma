using System;

namespace Irihi.Dogma.Controls.ViewModels;

public class BreadcrumbItemData(IObservable<string?> header)
{
    public IObservable<string?> Header => header;
}
