# Changelog

## 3.9.0

### Breaking change

- Pass `args` throughout all `Push` and `Show` lifecycle events

## 3.8.3

- Add static API `AnimationUpdateDeltaTime.Set(DeltaTimeType)` to change the type of delta time that is used by animation's `UpdateDispatcher`.

## 3.8.2

- Add option to set  alpha value of the `Image` component on `ModalBackdrop`

## 3.8.1

- Update `Samples~`

## 3.8.0

- Add `args` parameter to `Initialize` methods on `Activity`, `Modal`, `Screen` and `Sheet`
- Update other APIs to handle the new `args` parameter
- Some improvements on `Sheet` APIs
- Correct namespace of some classes and structs, might introduce breaking changes
- Fix some code styles

## 3.7.1

- Apply sorting layer id to newly created container layers
- Fix sorting layer id drawer doesn't update after the project's sorting layers are updated

## 3.7.0

- Properly apply `ContainerLayerSettings` to the newly created `ContainerLayer`s
- Allow applying custom `SortingLayer` and `OrderInLayer` to `Activity`
- Add `ScreenOptions` and `ActivityOptions` that include `WindowOptions` and more specialized options
- Add `PropertyDrawerFinder` to finds custom property drawer for a given type
- Add `ShowIfAttribute` to show serialized fields only if a condition is `true`
- Add `SortingLayerId` to show sorting layers as a dropdown
- Remove many unnecessary codes

## 3.6.1

- Add ability to switch between unscaled and scaled delta time (`UpdateDispatcher.SetDeltaTime`)

## 3.6.0

- Massive improvements on code styles and bug fixes
- Improve and correct `Activity` system
- Remove some unnecessary code

## 3.5.0

- Restructure the source code
- Rename namespaces to better separate between `Screen`, `Modal`, `Activity` classes and their namespace