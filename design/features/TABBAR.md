# TabBar

For a new player, the Museum tab is disabled as there are no collections open. The TabBar reacts to varoious events in the model.


## Something New Dot (SND)

This is a standard red dot seen in many games. When a tab has something new and wants to softly draw attention to it, this can be displayed on the tab button. When the tab is activated, SND disappears

## Received Events

* OnMuseumUnlocked: When receiving this signal, the MuseumTab becomes enabled and shows the SND
* OnCollectionUnlocked: When receiving this signal, the MuseumTab shows the SND
* OnItemUnlocked: MuseumTab shows SND

## Future considerations

We may want to force the player to tap on the Museum Tab

### A/B Tests

Later on when implementing analytics, we could do an A/B test for forced/unforced museum tab