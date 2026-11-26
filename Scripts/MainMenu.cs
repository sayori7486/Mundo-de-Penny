using Godot;

public partial class MainMenu : Control
{
	private const string PrologueScenePath = "res://Scenes/prologue.tscn";
	private const string MenuMusicPath = "res://Music/Evans Full.wav";
	private const string SettingsPath = "user://settings.cfg";
	private const float MusicFadeDuration = 1.0f;
	private const float SelectionPadding = 1.0f;
	private const string MasterBusName = "Master";

	private TextureButton _playButton = null!;
	private TextureButton _optionsButton = null!;
	private TextureButton _quitButton = null!;
	private Button _creditsButton = null!;
	private Panel _selectionOutline = null!;
	private VBoxContainer _menuButtons = null!;
	private PanelContainer _optionsPanel = null!;
	private PanelContainer _creditsPanel = null!;
	private HSlider _masterVolumeSlider = null!;
	private Label _masterVolumeValue = null!;
	private CheckButton _fullscreenToggle = null!;
	private Button _backButton = null!;
	private Button _creditsBackButton = null!;
	private ColorRect _transitionFade = null!;
	private readonly ConfigFile _settings = new();
	private Window.ModeEnum _windowedMode = Window.ModeEnum.Maximized;
	private int _masterBusIndex;
	private bool _startTransitioning;

	public override void _Ready()
	{
		_playButton = GetNode<TextureButton>("MenuButtons/PlayButton");
		_optionsButton = GetNode<TextureButton>("MenuButtons/OptionsButton");
		_quitButton = GetNode<TextureButton>("MenuButtons/QuitButton");
		_creditsButton = GetNode<Button>("OptionsPanel/Contents/CreditsButton");
		_selectionOutline = GetNode<Panel>("SelectionOutline");
		_menuButtons = GetNode<VBoxContainer>("MenuButtons");
		_optionsPanel = GetNode<PanelContainer>("OptionsPanel");
		_creditsPanel = GetNode<PanelContainer>("CreditsPanel");
		_masterVolumeSlider = GetNode<HSlider>("OptionsPanel/Contents/VolumeRow/MasterVolume");
		_masterVolumeValue = GetNode<Label>("OptionsPanel/Contents/VolumeRow/VolumeValue");
		_fullscreenToggle = GetNode<CheckButton>("OptionsPanel/Contents/FullscreenToggle");
		_backButton = GetNode<Button>("OptionsPanel/Contents/BackButton");
		_creditsBackButton = GetNode<Button>("CreditsPanel/Contents/BackButton");
		_transitionFade = GetNode<ColorRect>("TransitionFade");

		_masterBusIndex = AudioServer.GetBusIndex(MasterBusName);
		if (_masterBusIndex < 0)
		{
			GD.PushError($"MainMenu could not find the '{MasterBusName}' audio bus.");
		}

		_windowedMode = GetWindow().Mode;
		if (_windowedMode == Window.ModeEnum.Fullscreen)
		{
			_windowedMode = Window.ModeEnum.Maximized;
		}

		LoadSettings();
		_ = GetNode<MusicManager>("/root/MusicManager")
			.TransitionToTrack(MenuMusicPath, MusicFadeDuration);
		ConnectSignals();
		CallDeferred(nameof(FocusPlayButton));
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
		{
			return;
		}

		if (_creditsPanel.Visible)
		{
			CloseCredits();
		}
		else if (_optionsPanel.Visible)
		{
			CloseOptions();
		}
		else
		{
			return;
		}

		GetViewport().SetInputAsHandled();
	}

	private void LoadSettings()
	{
		Error loadError = _settings.Load(SettingsPath);
		if (loadError != Error.Ok && loadError != Error.FileNotFound)
		{
			GD.PushWarning($"Could not load menu settings '{SettingsPath}': {loadError}");
		}

		double savedVolume = _settings.GetValue("audio", "master_volume", 100.0).AsDouble();
		double volume = Mathf.Clamp((float)savedVolume, 0.0f, 100.0f);
		_masterVolumeSlider.Value = volume;
		_masterVolumeValue.Text = $"{Mathf.RoundToInt((float)volume)}%";
		ApplyMasterVolume((float)volume);

		bool fullscreen = _settings.GetValue("display", "fullscreen", false).AsBool();
		_fullscreenToggle.ButtonPressed = fullscreen;
		if (fullscreen)
		{
			GetWindow().Mode = Window.ModeEnum.Fullscreen;
		}
	}

	private void ConnectSignals()
	{
		_playButton.Pressed += StartGame;
		_optionsButton.Pressed += OpenOptions;
		_quitButton.Pressed += QuitGame;
		_creditsButton.Pressed += OpenCredits;
		_backButton.Pressed += CloseOptions;
		_creditsBackButton.Pressed += CloseCredits;
		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_fullscreenToggle.Toggled += OnFullscreenToggled;

		_playButton.FocusEntered += () => UpdateSelectionOutline(_playButton);
		_optionsButton.FocusEntered += () => UpdateSelectionOutline(_optionsButton);
		_quitButton.FocusEntered += () => UpdateSelectionOutline(_quitButton);
		_playButton.MouseEntered += () => _playButton.GrabFocus();
		_optionsButton.MouseEntered += () => _optionsButton.GrabFocus();
		_quitButton.MouseEntered += () => _quitButton.GrabFocus();
	}

	private void FocusPlayButton()
	{
		_playButton.GrabFocus();
	}

	private void UpdateSelectionOutline(Control selectedButton)
	{
		Rect2 buttonRect = selectedButton.GetGlobalRect();
		_selectionOutline.GlobalPosition = buttonRect.Position - new Vector2(SelectionPadding, SelectionPadding);
		_selectionOutline.Size = buttonRect.Size + new Vector2(SelectionPadding * 2.0f, SelectionPadding * 2.0f);
		_selectionOutline.Visible = true;
	}

	private async void StartGame()
	{
		if (_startTransitioning)
		{
			return;
		}

		_startTransitioning = true;
		SaveSettings();

		_playButton.Disabled = true;
		_optionsButton.Disabled = true;
		_quitButton.Disabled = true;
		_transitionFade.Visible = true;

		Tween fadeOut = CreateTween();
		fadeOut.TweenProperty(_transitionFade, "color:a", 1.0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		Error error = GetTree().ChangeSceneToFile(PrologueScenePath);
		if (error != Error.Ok)
		{
			GD.PushError($"Could not open prologue scene '{PrologueScenePath}': {error}");
			_startTransitioning = false;
			_playButton.Disabled = false;
			_optionsButton.Disabled = false;
			_quitButton.Disabled = false;

			Tween fadeIn = CreateTween();
			fadeIn.TweenProperty(_transitionFade, "color:a", 0.0f, 0.3f)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.InOut);
			await ToSignal(fadeIn, Tween.SignalName.Finished);
			_transitionFade.Visible = false;
		}
	}

	private void OpenOptions()
	{
		_menuButtons.Visible = false;
		_selectionOutline.Visible = false;
		_optionsPanel.Visible = true;
		_masterVolumeSlider.GrabFocus();
	}

	private void OpenCredits()
	{
		_optionsPanel.Visible = false;
		_creditsPanel.Visible = true;
		_creditsBackButton.GrabFocus();
	}

	private void CloseCredits()
	{
		_creditsPanel.Visible = false;
		_optionsPanel.Visible = true;
		_creditsButton.GrabFocus();
	}

	private void CloseOptions()
	{
		SaveSettings();
		_creditsPanel.Visible = false;
		_optionsPanel.Visible = false;
		_menuButtons.Visible = true;
		_optionsButton.GrabFocus();
	}

	private void QuitGame()
	{
		SaveSettings();
		GetTree().Quit();
	}

	private void OnMasterVolumeChanged(double value)
	{
		float volume = Mathf.Clamp((float)value, 0.0f, 100.0f);
		_masterVolumeValue.Text = $"{Mathf.RoundToInt(volume)}%";
		ApplyMasterVolume(volume);
		_settings.SetValue("audio", "master_volume", volume);
		SaveSettings();
	}

	private void ApplyMasterVolume(float volumePercent)
	{
		if (_masterBusIndex < 0)
		{
			return;
		}

		float linearVolume = volumePercent / 100.0f;
		float volumeDb = linearVolume <= 0.0001f ? -80.0f : Mathf.LinearToDb(linearVolume);
		AudioServer.SetBusVolumeDb(_masterBusIndex, volumeDb);
	}

	private void OnFullscreenToggled(bool fullscreen)
	{
		if (fullscreen)
		{
			if (GetWindow().Mode != Window.ModeEnum.Fullscreen)
			{
				_windowedMode = GetWindow().Mode;
			}

			GetWindow().Mode = Window.ModeEnum.Fullscreen;
		}
		else if (GetWindow().Mode == Window.ModeEnum.Fullscreen)
		{
			GetWindow().Mode = _windowedMode;
		}

		_settings.SetValue("display", "fullscreen", fullscreen);
		SaveSettings();
	}

	private void SaveSettings()
	{
		Error saveError = _settings.Save(SettingsPath);
		if (saveError != Error.Ok)
		{
			GD.PushError($"Could not save menu settings '{SettingsPath}': {saveError}");
		}
	}
}
