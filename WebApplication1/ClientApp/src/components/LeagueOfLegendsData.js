import React, { Component } from 'react';
import { Navbar, Button, ButtonToolbar, Table } from 'react-bootstrap';
import './Style/SpecificTeamStyle.css';
import Autosuggest from 'react-autosuggest';
import './Utilities';
import { Utilities } from './Utilities';
import ReactDOM from 'react-dom';
import { Results } from './RenderTabs/Results';
import { Prelive } from './RenderTabs/Prelive';
import { Teams } from './RenderTabs/Teams';
import { SpecificTeam } from './RenderTabs/SpecificTeam';
import { Players } from './RenderTabs/Players';
import { Leagues } from './RenderTabs/Leagues';
export class LeagueOfLegendsData extends Component {
   

    displayName = LeagueOfLegendsData.name

  constructor(props) {
      super(props);
      this.state = {
          Utilities: new Utilities(),
          sport: [],
          results: [],
          images: [],
          prelive: [],
          teams: [],
          players: [],
          playerStats: [],
          specificTeams: [],
          loading: true,
          loadedspecificLeague: false,
          shouldLoadPrelive: false,
          shouldLoadResults: false,
          shouldLoadTeams: false,
          shouldLoadSpecificTeam: false,
          shouldloadPlayers: false,
          specificTeam: [],
          playersInLeague: [],
          specificleague: '',
          haveSortedTeamsInLeague: false,
          ascending: true,
          value: '',
          searchTeamResults: [],
      };     
      
      this.sortBy = this.sortBy.bind(this);
      this.sortStatsBy = this.sortStatsBy.bind(this);


      this.compareBy = this.compareBy.bind(this);
      this.onSuggestionsFetchRequested = this.onSuggestionsFetchRequested.bind(this);
      this.onSuggestionsClearRequested = this.onSuggestionsClearRequested.bind(this);
      this.getSuggestionValue = this.getSuggestionValue.bind(this);
      this.renderSuggestion = this.renderSuggestion.bind(this);

    }
    componentDidMount() {
        let urlParam = this.props.location.pathname.split("/");
        if (urlParam.length === 3) {
            fetch('api/LeagueOfLegends/GetSport')
                .then(response => response.json())
                .then(data => {
                    this.setState({
                        sport: data,
                        results: data.resultsEvents,
                        images: data.teamLogos,
                        prelive: data.preliveEvents,
                        teams: data.teamsInLeague,
                        players: data.players,
                        playerStats: data.championStats,
                        loading: false,
                        specificleague: this.props.location.pathname.split('/')[2],
                        shouldLoadResults: true,
                        loadedspecificLeague: true,
                    });
                });
        } else {
            fetch('api/LeagueOfLegends/GetSport')
                .then(response => response.json())
                .then(data => {
                    this.setState({
                        sport: data,
                        results: data.resultsEvents,
                        images: data.teamLogos,
                        prelive: data.preliveEvents,
                        teams: data.teamsInLeague,
                        players: data.players,
                        playerStats: data.championStats,
                        loading: false,
                        specificleague: '',
                        shouldLoadResults: false,
                        loadedspecificLeague: false,
                    });
                });
        }
    }
    
    renderMenuTabs() {
        let specificTeams = this.state.teams[this.state.specificleague];
        let disablePrelive = true;
        if (this.state.specificleague in this.state.prelive) {
            disablePrelive = false;
        }
        let disableResults = true;
        if (this.state.specificleague in this.state.results) {
            disableResults = false;
        }
        return (
            <ButtonToolbar>

                <Button variant="outline-dark" onClick={() =>
                    this.setState({
                        loadedspecificLeague: false,
                        specificleague: '',
                        playersInLeague: [],
                        shouldLoadPrelive: false,
                        shouldLoadTeams: false,
                        shouldLoadSpecificTeam: false,
                        shouldloadPlayers: false,
                    })}>Back To All Leagues</Button>

                <Button variant="outline-dark" disabled={disableResults} onClick={() => this.setState({
                    shouldLoadResults: true,
                    shouldLoadPrelive: false,
                    shouldLoadTeams: false,
                    shouldLoadSpecificTeam: false,
                    shouldloadPlayers: false,
                    playersInLeague: [],
                    haveSortedTeamsInLeague: false,
                })}>Results</Button> 

                <Button variant="outline-dark" disabled={disablePrelive} onClick={() => this.setState({
                    shouldLoadPrelive: true,
                    shouldLoadResults: false,
                    shouldLoadTeams: false,
                    shouldLoadSpecificTeam: false,
                    shouldloadPlayers: false,
                    playersInLeague: [],
                })}>Prelive</Button>

                {specificTeams ? <Button variant="outline-dark" onClick={() => this.setState({
                    specificTeams: specificTeams,
                    shouldLoadPrelive: false,
                    shouldLoadResults: false,
                    shouldLoadSpecificTeam: false,
                    shouldLoadTeams: true,
                    shouldloadPlayers: false,
                    playersInLeague: [],
                })}>Teams</Button>

                    : <Button variant="outline-dark" disabled={true}>Teams</Button>}

                {specificTeams ? <Button variant="outline-dark" onClick={() => this.setState({
                    shouldLoadPrelive: false,
                    shouldLoadResults: false,
                    shouldLoadSpecificTeam: false,
                    shouldLoadTeams: false,
                    shouldloadPlayers: true,
                    playersInLeague: [],
                })}>Players</Button>

                    : <Button variant="outline-dark" disabled={true}>Players</Button>}

                <Button variant="outline-dark" onClick={() => navigator.clipboard.writeText(
                    this.props.location.protocol + this.props.location.origin + "/leagueoflegends/" + this.state.Utilities.replaceAll(this.state.specificleague," ","%20"))}
                    >Copy League URL to Clipboard</Button>
            </ButtonToolbar>
            );
    }            

    compareBy(key) {
        if (this.state.ascending) {
            return function (a, b) {

                if (a[key] > b[key]) return -1;
                if (a[key] < b[key]) return 1;
                return 0;
            }
        } else {
            return function (a, b) {

                if (a[key] < b[key]) return -1;
                if (a[key] > b[key]) return 1;
                return 0;
            }
        }
    }

    sortBy(key,array) {
        let arrayCopy = array;
        arrayCopy.sort(this.compareBy(key));
        if (this.state.ascending) {
            this.setState({ playersInLeague: arrayCopy, ascending: false });
        } else {
            this.setState({ playersInLeague: arrayCopy, ascending: true });
        }
    }
    sortStatsBy(key, array, playerName) {
        let arrayCopy = array;
        let indexOfInterest = 0;
        for (let i = 0; i < array.length; i++) {
            if (array[i][0].player.nickname === playerName) {
                indexOfInterest = i;
                break;
            }
        }
        arrayCopy[indexOfInterest].sort(this.compareBy(key));
        if (this.state.ascending) {
            this.setState({ playersInLeague: arrayCopy, ascending: false });
        } else {
            this.setState({ playersInLeague: arrayCopy, ascending: true });
        }
    }
    onSuggestionsFetchRequested(team) {
        let viableTeams = [];
        let allTeams = this.state.teams;
        for (var key in this.state.teams) {

            allTeams[key].forEach(function (t) {
                if (t.name.toLowerCase().includes(team.value.toLowerCase())) {
                    viableTeams.push(t);
                }
            });
        }
        this.setState({
            searchTeamResults: viableTeams,
        });
    }
    onSuggestionsClearRequested() {
        this.setState({
            searchTeamResults: []
        });
    }
    getSuggestionValue(suggestion) {
        return suggestion.name;
    }
    renderSuggestion(suggestion) {
        return (
            <div onClick={() => this.setState({
                shouldLoadSpecificTeam: true,
                specificTeam: suggestion,
                specificleague: this.state.Utilities.findTeamSpecificLeague(suggestion.name, this.state.teams),
                shouldLoadPrelive: false,
                shouldLoadResults: false,
                shouldLoadTeams: false,
                shouldloadPlayers: false,
                playersInLeague: [],
                haveSortedTeamsInLeague: false,
                loadedspecificLeague: true,
            })}><img alt=''                  
                    src={this.state.Utilities.getImageString(this.state.images, suggestion.name)} alt='teamImage'></img>
                {suggestion.name}
            </div>
        );
    }
    onChange = (event, { newValue, method }) => {
        this.setState({
            value: newValue
        });
    };      

    clickedOnTeam = (team) => {
        this.setState({
            shouldLoadResults: false,
            shouldLoadPrelive: false,
            shouldLoadTeams: false,
            shouldLoadSpecificTeam: true,
            specificTeam: team,
            haveSortedTeamsInLeague: false,
        });
    }
    clickedOnLeague = (league) => {
        this.setState({
            loadedspecificLeague: true,
            shouldLoadResults: true,
            specificleague: league
        });
    }

render() {
    let contents;
    let menuTabs;
    const { value } = this.state;
    const inputProps = {
        placeholder: "Find Team",
        value,
        onChange: this.onChange,
    };
    if (this.state.shouldLoadSpecificTeam) {
        menuTabs = this.renderMenuTabs();
        contents = (
            <SpecificTeam
                specificTeam={this.state.specificTeam}
                images={this.state.images}
                specificleague={this.state.specificleague}
                players={this.state.players}
                sortStatsBy={this.sortStatsBy}
                playerStats={this.state.playerStats}
            />
        );
    }
    else if (this.state.shouldLoadResults && this.state.specificleague === '') {
        this.setState({
            shouldLoadResults: false,
            loadedspecificLeague: false,
        })
    }
    else if (this.state.loadedspecificLeague) {
        menuTabs = this.renderMenuTabs();
        if (this.state.shouldLoadResults) {
            if (!this.state.results[this.state.specificleague]) {
                this.setState({
                    shouldLoadResults: false,
                    shouldLoadPrelive: true,
                });
            } else {
                contents = (
                    <Results
                        results={this.state.results[this.state.specificleague]}
                        images={this.state.images}
                        specificleague={this.state.specificleague}
                    />
                );
            }
        } else if (this.state.shouldLoadPrelive) {
            if (!this.state.prelive[this.state.specificleague]) {
                this.setState({
                    shouldLoadTeams: true,
                    shouldLoadPrelive: false,
                    specificTeams: this.state.teams[this.state.specificleague],
                    playersInLeague: [],
                });
            } else {
                contents = (
                    <Prelive
                        prelive={this.state.prelive[this.state.specificleague]}
                        images={this.state.images}
                        specificleague={this.state.specificleague}
                    />
                );
            }
        } else if (this.state.shouldLoadTeams) {
            if (!this.state.specificTeams) {
                this.setState({
                    loadedspecificLeague: false,
                    specificleague: '',
                    playersInLeague: [],
                    shouldLoadPrelive: false,
                    shouldLoadTeams: false,
                    shouldLoadSpecificTeam: false,
                    shouldloadPlayers: false,
                })
            } else {
                contents = (
                    <Teams
                        teams={this.state.specificTeams}
                        images={this.state.images}
                        specificleague={this.state.specificleague}
                        sortBy={this.sortBy}
                        clickedOnTeam={this.clickedOnTeam}
                    />
                );
            }
        } else if (this.state.shouldloadPlayers) {
            contents = (
                <Players
                    players={this.state.players}
                    teamsInLeague={this.state.teams[this.state.specificleague]}
                    specificleague={this.state.specificleague}
                    sortBy={this.sortBy}
                />
            );
        }

    } else {       
        contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : <Leagues
                data={this.state.sport}
                gameName="League of Legends"
                clickedOnLeague={this.clickedOnLeague}
            />
    }

    return (
        <div>     
            <Autosuggest
                suggestions={this.state.searchTeamResults}
                onSuggestionsFetchRequested={this.onSuggestionsFetchRequested}
                onSuggestionsClearRequested={this.onSuggestionsClearRequested}
                getSuggestionValue={this.getSuggestionValue}
                renderSuggestion={this.renderSuggestion}
                inputProps={inputProps} />
            {menuTabs}
            {contents}
      </div>
    );
  }
}
