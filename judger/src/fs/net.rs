//! Functions to download stuff into destinations
use futures::{SinkExt, StreamExt};
use std::fmt::Write;
use std::path::Path;
use std::str::FromStr;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::process::Command;

#[derive(Debug)]
pub struct GitCloneOptions {
    pub repo: String,
    pub revision: String,
    // pub branch: Option<String>,
    pub depth: usize,
}

impl Default for GitCloneOptions {
    fn default() -> Self {
        GitCloneOptions {
            repo: String::new(),
            revision: String::new(),
            // branch: Some(String::from("master")),
            depth: 5,
        }
    }
}

macro_rules! do_command {
    ($($dir:expr,)? [ $cmd:expr, $($arg:expr),*]) => {

        let cmd = Command::new($cmd)
            $(.current_dir($dir))?
            .args(&[$($arg),*])
            .status()
            .await?;

        if !cmd.success(){
            let mut format_string = String::new();
            write!(format_string, "Command failed: `{}",$cmd).unwrap();
            $(
                write!(format_string, " {}",$arg).unwrap();
            )*
            write!(format_string, "` returned {:?}",cmd.code()).unwrap();
            return Err(std::io::Error::new(std::io::ErrorKind::InvalidInput, format_string))
        }
    };
}

pub async fn git_clone(dir: &Path, options: GitCloneOptions) -> std::io::Result<()> {
    // This clone procedure follows
    // https://stackoverflow.com/questions/3489173/how-to-clone-git-repository-with-specific-revision-changeset
    // to clone a single revision. This requires the server to directly
    // specify the commit ID or it won't work.

    // # make a new blank repository in the current directory
    // git init
    //
    // # add a remote
    // git remote add origin url://to/source/repository
    //
    // # fetch a commit (or branch or tag) of interest
    // # Note: the full history up to this commit will be retrieved unless
    // #       you limit it with '--depth=...' or '--shallow-since=...'
    // git fetch origin <sha1-of-commit-of-interest>
    //
    // # reset this repository's master branch to the commit of interest
    // git reset --hard FETCH_HEAD

    tokio::fs::create_dir_all(dir).await?;

    do_command!(dir, ["git", "init"]);
    do_command!(dir, ["git", "remote", "add", "origin", &options.repo]);
    do_command!(
        dir,
        ["git", "fetch", "origin", &options.revision, "--depth", "1"]
    );
    do_command!(dir, ["git", "reset", "--hard", "FETCH_HEAD", "--"]);
    do_command!(dir, ["git", "submodule", "init"]);
    do_command!(dir, ["git", "submodule", "update", "--recommend-shallow"]);

    Ok(())
}

pub async fn download_unzip(
    client: reqwest::Client,
    req: reqwest::Request,
    dir: &Path,
    temp_file_path: &Path,
) -> anyhow::Result<()> {
    let res: anyhow::Result<_> = async {
        log::info!(
            "Downloading from {} to {}",
            req.url(),
            temp_file_path.display()
        );
        let resp = client.execute(req).await?.error_for_status()?;
        let mut file = tokio::fs::File::create(temp_file_path).await?;

        let mut stream = resp.bytes_stream();

        while let Some(bytes) = stream.next().await {
            let bytes = bytes?;
            log::info!("Writing {} bytes into {}", bytes.len(), dir.display());
            file.write_all(&bytes).await?;
        }
        file.flush();
        drop(file);

        let unzip_res = Command::new("7z")
            .args(&[
                "x",
                &temp_file_path.to_string_lossy(),
                &format!("-o{}", dir.to_string_lossy()),
            ])
            .output()
            .await?;
        tokio::fs::remove_file(temp_file_path).await?;
        if unzip_res.status.success() {
            Ok(())
        } else {
            Err(std::io::Error::new(
                std::io::ErrorKind::InvalidData,
                format!(
                    "7zip failed to extract, exited with output:\n{}",
                    String::from_utf8_lossy(&unzip_res.stdout)
                ),
            )
            .into())
        }
    }
    .await;

    match res {
        Ok(_) => {}
        Err(_) => {
            // cleanup
            let _ = tokio::fs::remove_file(temp_file_path).await;
        }
    }

    res
}
