Title: .NET Foundation problems and solutions
Published: 05/10/2021
Tags:
- DNF
- .NET
---

## What's been happening behind the scenes

So as a lot of your know that has been a lot of controversy lately about the Dot Net Foundation (DNF).

There's been a bit happening behind the scenes. I would recommend reading [Rodney Littles II](https://rodneylittlesii.com/posts/topic/foundation-echo-chamber) post about his experiences on the board as well.

### Optimism

Be aware although there is a lot of bad press about projects at the moment that the .NET community is made up of a lot of fabulous creators who put their hard earned time and effort into projects. 

We really welcome all the people who put meaningful contributions into projects. One of the key things I know most maintainers will respect is if you put the effort into communication to make their job easier.

We do get donations through GitHub sponsors, which are definitely welcome, but this is definitely not enough to allow us to sustain a living. So for us OSS is about passion of helping a community, getting new skills that we may not in our day job, and the networking with other like minded people.

Please do not think you can't contribute to your favourite project at the moment. We are going through some politics at the moment but because it's becoming more public it's just about making our lives easier.

### Releases

One thing that has happened since the early day of the foundation is they have had a lot of teething problems. On ReactiveUI, during the early days, we were forced to use Azure DevOps along with the DNF active directory. None of the core maintainers could login reliably so therefore we weren't able to do our own releases. 

We kept bringing this up to the DNF staff and we kept getting months and months of issues telling us we were effectively crazy without problems. This is a theme that continued to this day.

A lot of our pressures were removed when we swapped to GitHub actions for our main release cycle. It also allowed us to assign releasers who could do the release for us and keep the project running without 1 or 2 key people being available.

Recently we were unable to release for 3 months, it wasn't until Geoff Huntley the past lead maintainer posted on twitter that we got the problem resolved. 

A current point of pain for us is the SignClient, it fails, can't handle large amounts of requests very easily.

<?# Twitter 1422505990486851585 /?>

Again, we had a lot of problems with getting it recognised we weren't "crazy".

I would estimate in the 4 years I have been lead maintainer with Rodney we been able to do 3 years of actual releases. 

### GitHub enterprise

About 6 months ago we decided as maintainers on the ReactiveUI we wanted to move over to [GitHub sponsors](https://github.com/sponsors/reactivemarbles). At this time we saw the legal entity was already filled in, when previous a week before it wasn't, and the legal entity was set to "Dot Net Foundation Inc".

I then looked at our main repository and saw we had moved over to the "Dot Net Foundation" enterprise GitHub repository rather than the public one. 

We had to make a emergency decision and we made the decision to make our GitHub sponsorships go through a organisation ReactiveMarbles where we been developing new code separate to the DNF.

![Image to GitHub Enterprise Settings](../images/enterprise-github.png)

We weren't sure the implications of the change on sponsorships.

We also reached out a number of other DNF member projects and they had their GitHub changed from the public repository as well.

None of this to this day has been adequately communicated to foundation projects.

So for projects who want to be able to make themselves more sustainable this is a big problem. 

### NuGet packages

3 months ago we had repeated problems with SignClient. SignClient is a project of the DNF, which allows for signing certificates to be signed remotely from a central server. Allow some abstraction from the certificate itself.

We couldn't get our NuGet packages signed again, about the 6th problem we had since I took over the project 4 years ago.

We already had a non-profit organisation for conducting training, so we decided to get a code signing certificate for it.

Great we got it.

Went to change our signing certificate for some reason our signing organisation was "locked" to "Dot Net Foundation".

![Image to Nuget Settings](../images/lock-out-nuget.png)

This meant effectively we couldn't use our certificate registered to the "ReactiveUI Assocation Inc".

If you think about what this means it effectively means if the DotNetFoundation doesn't like something you did they control the signing with SignClient, they control the signing certificate on NuGet, so they can block any release you do.

So the question I have to ask is this change the best for member projects? We have much more aggreesive enforcement of policies that haven't been communicated well. OSS thrives on the ability to set our own policies that match where our teams are at. The recent YOLO PR and commit highlights in general the DNF needs more transparency around this.

Having forced settings work well for internal Microsoft projects like dotnet itself but maybe not for smaller volunteer run projects like ReactiveUI.

The NuGet team are now chasing up what's happening in regards to this at the time of writing this article.

### Going forward

OSS maintainers like to feel respected and at the moment we don't feel that respect from the DNF. A number of large scale changes have been made that feel more corporate driven that require us to match SLAs set by Microsoft.

I have at the request of the DNF Board emailed them all the communications and different issues we been having. Some of the DNF board elect are well respected and liked open source contributors. 

We as a project in general have decided not to give the DNF any benefits of the doubt anymore and make information public where there needs to be a lot more open discussion.

We also don't want to come across as attacking the DNF, we have had some very big and major issues but we are working with the board to make sure these issues are resolved but we also just need this information out in the public so people are aware.

It's also important to note several board members have acknowledged the information I have given them and said it will be high priority at their next board meeting.

Let's work as a community to find the best solution to this problem. That might be with the DNF or not, but as a community we should help each other out.