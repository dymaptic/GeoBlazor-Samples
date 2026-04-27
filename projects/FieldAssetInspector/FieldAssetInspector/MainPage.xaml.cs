using FieldAssetInspector.Razor;
using FieldAssetInspector.Razor.Models;

namespace FieldAssetInspector;

public sealed partial class MainPage : Page
{
    private readonly AssetSelectionService _selection;

    public MainPage(AssetSelectionService selection)
    {
        _selection = selection;
        this.InitializeComponent();

        _selection.AssetSelected += OnAssetSelected;
        _selection.SelectionCleared += OnSelectionCleared;
        Unloaded += (_, _) =>
        {
            _selection.AssetSelected -= OnAssetSelected;
            _selection.SelectionCleared -= OnSelectionCleared;
        };
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
    /// Called from the Blazor map component (via AssetSelectionService) when a feature is selected.
    /// Updates the Uno XAML sidebar with the selected asset's attributes.
    /// </summary>
    private void OnAssetSelected(FieldAsset asset)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AssetInfoText.Visibility = Visibility.Collapsed;
            AssetDetailsPanel.Visibility = Visibility.Visible;

            AssetIdField.Text = asset.ObjectId;
            AssetTypeField.Text = asset.AssetType;
            AssetNotesField.Text = string.Empty;

            // Build editable TextBoxes for each attribute
            AttributeFieldsPanel.Children.Clear();
            foreach ((string key, object? value) in asset.Attributes)
            {
                if (key == asset.ObjectIdField)
                {
                    // we already posted the ObjectID at the top
                    continue;
                }
                
                TextBox textBox = new()
                {
                    Header = key,
                    Text = value?.ToString() ?? string.Empty,
                    PlaceholderText = "—"
                };
                
                AttributeFieldsPanel.Children.Add(textBox);
            }
        });
    }

    private void OnSelectionCleared()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AssetInfoText.Visibility = Visibility.Visible;
            AssetDetailsPanel.Visibility = Visibility.Collapsed;
            AttributeFieldsPanel.Children.Clear();
        });
    }
}
