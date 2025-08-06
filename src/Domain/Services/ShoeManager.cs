using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Domain.Services;

/// <summary>
/// Manages shoe operations and automatic reshuffling.
/// </summary>
public class ShoeManager : IShoeManager
{
    private IShoe? _currentShoe;
    private bool _autoReshuffleEnabled = true;
    private double _penetrationThreshold = 0.25;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShoeManager"/> class.
    /// </summary>
    public ShoeManager()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShoeManager"/> class with a shoe.
    /// </summary>
    /// <param name="shoe">The shoe to manage.</param>
    public ShoeManager(IShoe shoe)
    {
        Initialize(shoe);
    }

    /// <inheritdoc />
    public IShoe CurrentShoe => _currentShoe ?? throw new InvalidOperationException("Shoe manager has not been initialized with a shoe.");

    /// <inheritdoc />
    public bool AutoReshuffleEnabled
    {
        get => _autoReshuffleEnabled;
        set
        {
            _autoReshuffleEnabled = value;
            if (_currentShoe is Entities.Shoe shoe)
            {
                shoe.AutoReshuffleEnabled = value;
            }
        }
    }

    /// <inheritdoc />
    public double PenetrationThreshold
    {
        get => _penetrationThreshold;
        set
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Penetration threshold must be between 0.0 and 1.0.");
            }
            
            _penetrationThreshold = value;
            if (_currentShoe is Entities.Shoe shoe)
            {
                shoe.PenetrationThreshold = value;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ShoeReshuffleEventArgs>? ReshuffleOccurred;

    /// <inheritdoc />
    public event EventHandler<ShoeReshuffleEventArgs>? ReshuffleRequired;

    /// <inheritdoc />
    public void Initialize(IShoe shoe)
    {
        if (shoe == null)
        {
            throw new ArgumentNullException(nameof(shoe));
        }

        // Unsubscribe from previous shoe events if any
        if (_currentShoe != null)
        {
            _currentShoe.ReshuffleNeeded -= OnShoeReshuffleNeeded;
            _currentShoe.Reshuffled -= OnShoeReshuffled;
        }

        _currentShoe = shoe;

        // Configure shoe settings
        if (_currentShoe is Entities.Shoe shoeEntity)
        {
            shoeEntity.AutoReshuffleEnabled = _autoReshuffleEnabled;
            shoeEntity.PenetrationThreshold = _penetrationThreshold;
        }

        // Subscribe to shoe events
        _currentShoe.ReshuffleNeeded += OnShoeReshuffleNeeded;
        _currentShoe.Reshuffled += OnShoeReshuffled;
    }

    /// <inheritdoc />
    public bool HandleAutomaticReshuffle()
    {
        if (_currentShoe == null)
        {
            throw new InvalidOperationException("Shoe manager has not been initialized with a shoe.");
        }

        if (!_autoReshuffleEnabled)
        {
            return false;
        }

        if (IsReshuffleNeeded())
        {
            var remainingPercentage = _currentShoe.GetRemainingPercentage();
            
            // Perform the reshuffle
            _currentShoe.Reset();
            
            var eventArgs = new ShoeReshuffleEventArgs(
                remainingPercentage,
                _penetrationThreshold,
                "Automatic reshuffle performed by ShoeManager");

            OnReshuffleOccurred(eventArgs);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void TriggerManualReshuffle(string reason = "Manual reshuffle")
    {
        if (_currentShoe == null)
        {
            throw new InvalidOperationException("Shoe manager has not been initialized with a shoe.");
        }

        var remainingPercentage = _currentShoe.GetRemainingPercentage();
        
        // Perform the reshuffle
        if (_currentShoe is Entities.Shoe shoe)
        {
            shoe.TriggerReshuffle(reason);
        }
        else
        {
            _currentShoe.Reset();
            var eventArgs = new ShoeReshuffleEventArgs(remainingPercentage, _penetrationThreshold, reason);
            OnReshuffleOccurred(eventArgs);
        }
    }

    /// <inheritdoc />
    public bool IsReshuffleNeeded()
    {
        if (_currentShoe == null)
        {
            return false;
        }

        return _currentShoe.NeedsReshuffle();
    }

    /// <summary>
    /// Handles the shoe's ReshuffleNeeded event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnShoeReshuffleNeeded(object? sender, ShoeReshuffleEventArgs e)
    {
        if (_autoReshuffleEnabled)
        {
            // Automatically handle the reshuffle
            HandleAutomaticReshuffle();
        }
        else
        {
            // Notify that a reshuffle is required but not automatically handled
            OnReshuffleRequired(e);
        }
    }

    /// <summary>
    /// Handles the shoe's Reshuffled event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnShoeReshuffled(object? sender, ShoeReshuffleEventArgs e)
    {
        OnReshuffleOccurred(e);
    }

    /// <summary>
    /// Raises the ReshuffleOccurred event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnReshuffleOccurred(ShoeReshuffleEventArgs e)
    {
        ReshuffleOccurred?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the ReshuffleRequired event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnReshuffleRequired(ShoeReshuffleEventArgs e)
    {
        ReshuffleRequired?.Invoke(this, e);
    }
}