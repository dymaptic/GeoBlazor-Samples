namespace FieldAssetInspector;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void OnSaveChangesClick(object sender, RoutedEventArgs e)
    {
        // In a real application, this would persist changes to the feature layer
        // via GeoBlazor's ApplyEdits functionality.
        var dialog = new ContentDialog
        {
            Title = "Changes Saved",
            Content = "Asset attributes have been updated successfully.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    /// <summary>
    /// Called from the Blazor map component (via JS interop bridge) when a feature is selected.
    /// Updates the Uno XAML sidebar with the selected asset's attributes.
    /// </summary>
    public void OnAssetSelected(string assetId, string assetType, string status)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AssetInfoText.Visibility = Visibility.Collapsed;
            AssetDetailsPanel.Visibility = Visibility.Visible;

            AssetIdField.Text = assetId;
            AssetTypeField.Text = assetType;
            AssetStatusField.Text = status;
            AssetNotesField.Text = string.Empty;
        });
    }
}
