# Baseline Snapshots

Golden outputs of the **legacy** (.NET 7) extraction pipeline against
`samples/sample-mod/`. The rewrite must reproduce these byte-for-byte.

## Regenerate

If `samples/sample-mod/` changes intentionally:

1. Delete the stale snapshot:
   `rm tests/__snapshots__/legacy/sample-mod.extraction.json`
2. Run the legacy baseline test — it writes the new snapshot and fails.
3. Inspect the new snapshot manually.
4. Commit fixture + new snapshot in the same commit.

Never edit a snapshot by hand.
